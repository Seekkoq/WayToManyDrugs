using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using UnityEngine;

using WvcLog = CustomNPCExample.Utils.WvcLog;

using NativeItemInstance = Il2CppScheduleOne.ItemFramework.ItemInstance;
using NativeItemSlot = Il2CppScheduleOne.ItemFramework.ItemSlot;
using NativePackaging = Il2CppScheduleOne.Product.Packaging.PackagingDefinition;
using NativeProductInstance = Il2CppScheduleOne.Product.ProductItemInstance;
using NativeStorableDefinition = Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// Teaches the vanilla Brick Press to work with Xanax Powder.
    ///
    /// Powder is loaded into the press by hand and pressed the way a product is: the press reports
    /// that it is ready, the player presses BEGIN, and the powder becomes the same number of Xanax
    /// bars, straight into the press's output slot. There is no pouring, no handle game and no brick.
    ///
    /// 1. The press's slots accept the powder item (vanilla only accepts products).
    /// 2. A press holding powder reports that it is ready to begin, so BEGIN lights up and the player
    ///    is not left pressing a dead button; a press whose output is full says so with the game's
    ///    own "Output slot is full!" text.
    /// 3. The press's own reports about what it is loaded with have the powder taken out of them:
    ///    the press reads those reports as products and throws on anything else on its way to
    ///    deciding what to do. Without this it threw on every refresh of the interface as soon as
    ///    twenty units of powder sat in the machine, and the interface stopped updating.
    /// 4. BEGIN fills the output slot with bars made from the powder. Both the button's own handler
    ///    and the code that would start the press task are skipped, so the pour animation and the
    ///    handle minigame are never shown.
    /// 5. Powder that was dropped into an open interface is turned into bars as soon as the task
    ///    that would press it looks at the mould, so the mould never reaches the pouring or the
    ///    pressing step.
    /// 6. While a powder batch is being handled the press may not apply its brick packaging, and an
    ///    item that still came out as a brick is rewritten into loose bars.
    /// 7. One run takes what the output slot can give back, up to a ceiling of twenty units for
    ///    twenty bars, one to one. Powder past that is left in the machine for the next press rather
    ///    than being swallowed by a batch that never comes out.
    ///
    /// Every one of those rules is powder only. A press with no powder in it - one loaded with
    /// Xanax to be stamped into a brick included - is the game's own press and is left to the game:
    /// the brick it produces is the player's to keep.
    /// </summary>
    public static class XanaxBrickPressPatch
    {
        /// <summary>How long a press counts as "mid powder batch" before the mark is dropped.</summary>
        private const float RunWindow = 120f;

        private const string BrickPackagingId = "brick";

        /// <summary>
        /// The most a single press run may swallow, and the amount the run falls back to when the
        /// output slot cannot say what it holds.
        ///
        /// The machine itself will take more than this in one go, and the output slot will not give
        /// it back: the slot's own capacity is the real limit and is asked for first, so this is only
        /// the ceiling that keeps one absurd load from emptying the whole machine into one stack.
        /// </summary>
        public const int UnitsPerRun = 20;

        /// <summary>
        /// Bars a run produces. The press is one to one: a unit of powder is a bar.
        /// </summary>
        public static int BarsForUnits(int powderUnits)
        {
            return powderUnits > 0 ? powderUnits : 0;
        }

        private static HarmonyLib.Harmony _harmony;
        private static bool _applied;
        private static bool _failureLogged;

        /// <summary>Press instance id -> time the powder batch started.</summary>
        private static readonly Dictionary<int, float> PowderRuns = new Dictionary<int, float>();

        private static readonly HashSet<int> PackagingSuppressionLogged = new HashSet<int>();

        /// <summary>Presses whose powder load has already been noted in the log.</summary>
        private static readonly HashSet<int> PowderHiddenLogged = new HashSet<int>();

        private static MethodInfo _updateInputVisuals;
        private static MethodInfo _updateInterface;

        public static void ApplyPatch()
        {
            if (_applied)
                return;

            try
            {
                _harmony = new HarmonyLib.Harmony("wvc.xanax.brickpress");

                int patched = 0;

                // The press's own answer to "may I start?": powder is not a product, so without this
                // the press reports a missing product and greys BEGIN out instead of letting the
                // powder be pressed. See GetState_Prefix.
                patched += PatchByName(typeof(BrickPress), "GetState", 0, nameof(GetState_Prefix));

                patched += PatchByName(typeof(BrickPress), "CompletePress", 1, nameof(CompletePress_Prefix));
                patched += PatchPostfixByName(typeof(BrickPress), "CompletePress", 1, nameof(CompletePress_Postfix));
                patched += PatchByName(typeof(NativeProductInstance), "SetPackaging", 1, nameof(SetPackaging_Prefix));

                // The press's own routines report what is loaded in its input slots; a load of
                // powder is not a product, and the press would choke on it, so the powder is
                // kept out of that report. See GetMainInputs_Postfix.
                patched += PatchPostfixByName(
                    typeof(BrickPress),
                    "GetMainInputs",
                    4,
                    nameof(GetMainInputs_Postfix));

                // The press's own "Begin" button lives on the task, not on the press, so that is
                // where the instant hand-over has to hook in.
                patched += PatchByName(
                    typeof(Il2CppScheduleOne.PlayerTasks.UseBrickPress),
                    "BeginPress",
                    0,
                    nameof(BeginPress_Prefix));

                patched += PatchByName(
                    typeof(Il2CppScheduleOne.PlayerTasks.UseBrickPress),
                    "CheckMould",
                    0,
                    nameof(CheckMould_Prefix));

                // The BEGIN button of the press interface lives on the station canvas. Catching it
                // here - rather than in the task - means the press task is never even created, so
                // the pour animation and the handle minigame cannot flash on screen.
                patched += PatchByName(
                    typeof(Il2CppScheduleOne.UI.Stations.BrickPressCanvas),
                    "BeginButtonPressed",
                    0,
                    nameof(BeginButtonPressed_Prefix));

                patched += PatchByName(
                    typeof(Il2CppScheduleOne.UI.Stations.BrickPressCanvas),
                    "BeginTask",
                    1,
                    nameof(BeginTask_Prefix));

                foreach (MethodInfo method in typeof(NativeItemSlot).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method == null || method.IsSpecialName)
                        continue;

                    if (method.Name != "DoesItemMatchHardFilters" &&
                        method.Name != "DoesItemMatchPlayerFilters")
                    {
                        continue;
                    }

                    _harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(
                            typeof(XanaxBrickPressPatch),
                            nameof(SlotFilter_Prefix)));

                    patched++;


                }

                _updateInputVisuals = AccessTools.Method(typeof(BrickPress), "UpdateInputVisuals");

                _updateInterface = AccessTools.Method(
                    typeof(Il2CppScheduleOne.UI.Stations.BrickPressCanvas),
                    "UpdateUI");



                if (patched == 0)
                {

                    return;
                }

                _applied = true;


            }
            catch (Exception ex)
            {
                if (!_failureLogged)
                {
                    _failureLogged = true;
                    MelonLogger.Error("[WVC Powder Press] Patch setup failed: " + ex);
                }
            }
        }

        private static int PatchByName(Type type, string name, int parameterCount, string handler)
        {
            return PatchMethod(type, name, parameterCount, handler, false);
        }

        private static int PatchPostfixByName(Type type, string name, int parameterCount, string handler)
        {
            return PatchMethod(type, name, parameterCount, handler, true);
        }

        private static int PatchMethod(Type type, string name, int parameterCount, string handler, bool postfix)
        {
            int patched = 0;

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (method == null || method.IsSpecialName || method.Name.Contains("b__"))
                    continue;

                if (method.Name != name || method.GetParameters().Length != parameterCount)
                    continue;

                HarmonyMethod patch = new HarmonyMethod(typeof(XanaxBrickPressPatch), handler);

                if (postfix)
                    _harmony.Patch(method, postfix: patch);
                else
                    _harmony.Patch(method, prefix: patch);

                patched++;


            }

            return patched;
        }

        /// <summary>
        /// The press's own answer to "may I start?".
        ///
        /// The press works this out from what is in its feed slots, and it expects a product there:
        /// with powder loaded it reports that product is missing, which is what left BEGIN grey and
        /// the player unable to do anything with the powder they had put in.
        ///
        /// A press holding powder is told it may begin while its output still has room, so BEGIN
        /// lights up and the powder can be pressed. Once the output is full the press is told so,
        /// which puts the game's own "Output slot is full!" on the screen instead of the machine
        /// quietly doing nothing. A press with no powder in it is left entirely to the game.
        /// </summary>
        public static bool GetState_Prefix(
            BrickPress __instance,
            ref Il2CppScheduleOne.ObjectScripts.PackagingStation.EState __result)
        {
            try
            {
                if (__instance == null)
                    return true;

                // Powder only: a press with none in it is the game's own press, and it decides
                // everything about itself.
                if (!PressHoldsPowder(__instance))
                    return true;

                int waiting = BarCount(__instance);

                // The press asks this every time it looks at itself - each frame while its interface
                // is open - so the answer has to be the one that makes the machine usable: powder in
                // the press and room in the output means "you may begin".
                __result = OutputRoom(__instance, waiting) > 0
                    ? Il2CppScheduleOne.ObjectScripts.PackagingStation.EState.CanBegin
                    : Il2CppScheduleOne.ObjectScripts.PackagingStation.EState.OutputSlotFull;

                return false;
            }
            catch (Exception)
            {

                return true;
            }
        }

        /// <summary>
        /// Gathers every slot the press exposes. The powder is loaded by hand, so it can be sitting
        /// in the mould slots, the feed slots or the general item slots.
        /// </summary>
        private static void CollectSlots(BrickPress press, List<NativeItemSlot> into)
        {
            try
            {
                ItemSlot[] mould = press.ProductSlots;

                if (mould != null)
                {
                    for (int i = 0; i < mould.Length; i++)
                        AddSlot(into, mould[i]);
                }
            }
            catch (Exception)
            {

            }

            try
            {
                var inputs = press.InputSlots;

                if (inputs != null)
                {
                    for (int i = 0; i < inputs.Count; i++)
                        AddSlot(into, inputs[i]);
                }
            }
            catch (Exception)
            {

            }

            try
            {
                var items = press.ItemSlots;

                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                        AddSlot(into, items[i]);
                }
            }
            catch (Exception)
            {

            }
        }

        private static void AddSlot(List<NativeItemSlot> into, NativeItemSlot slot)
        {
            if (slot == null)
                return;

            for (int i = 0; i < into.Count; i++)
            {
                if (into[i] == slot)
                    return;
            }

            into.Add(slot);
        }

        /// <summary>
        /// Takes exactly the wanted number of units out of the slots they were counted from: whole
        /// slots are emptied first and the last one is split, so a machine holding more than the run
        /// pays for keeps what is left for the next press.
        ///
        /// Returns the number of units that really came out. A slot that refuses to be split is left
        /// alone rather than half taken, so the caller can shrink the batch to what actually moved.
        /// </summary>
        private static int TakeUnits(List<NativeItemSlot> filled, int wanted)
        {
            int left = wanted;

            for (int i = 0; i < filled.Count && left > 0; i++)
            {
                NativeItemSlot slot = filled[i];
                NativeItemInstance item = slot?.ItemInstance;

                if (item == null)
                    continue;

                int have = item.Quantity;

                if (have <= 0)
                    continue;

                try
                {
                    if (have <= left)
                    {
                        slot.ClearStoredInstance(false);
                        left -= have;



                        continue;
                    }

                    int keep = have - left;
                    int took = have - keep;

                    slot.SetQuantity(keep, false);

                    if ((slot.ItemInstance?.Quantity ?? 0) != keep)
                    {


                        continue;
                    }

                    left = 0;

                    try
                    {
                        slot.ReplicateStoredInstance();
                    }
                    catch
                    {
                    }


                }
                catch (Exception)
                {

                }
            }

            return wanted - left;
        }

        /// <summary>
        /// How many more bars the press's output slot can still hand back.
        ///
        /// The slot's own capacity for the item is the real limit, and it is what quietly swallowed
        /// the tail of an over-sized batch; an output holding somebody else's product is not counted
        /// as room at all, because the run must never throw away goods that are not ours.
        ///
        /// The slot is asked with a bar in hand, so an empty output still answers with what it can
        /// take: that is what lets a whole load be pressed in one go instead of ten units at a time
        /// with the rest left sitting in the machine.
        /// </summary>
        private static int OutputRoom(BrickPress press, int waiting)
        {
            int capacity = UnitsPerRun;

            try
            {
                NativeItemSlot output = press?.OutputSlot;

                if (output == null)
                    return UnitsPerRun;

                NativeItemInstance held = output.ItemInstance;

                if (held != null && !IsXanax(held))
                    return 0;

                if (held == null)
                {
                    if (!TryCreateItemInstance(Xanax.ProductId, 1, out NativeItemInstance probe) ||
                        probe == null)
                    {
                        return UnitsPerRun - waiting > 0 ? UnitsPerRun - waiting : 0;
                    }

                    held = probe;
                }

                int forItem = output.GetCapacityForItem(held, false);

                if (forItem > 0)
                    capacity = forItem;
            }
            catch
            {
            }

            int room = capacity - waiting;

            return room > 0 ? room : 0;
        }

        /// <summary>
        /// Keeps the powder out of the report the press makes about its own load.
        ///
        /// The press asks for its main inputs - what is sitting in its input slots - and then
        /// treats the answer as a product: it casts it, compares its quality against the mould's
        /// and asks whether twenty of them are there. Powder is not a product, so that cast is what
        /// broke the press: with twenty units or more of powder in the input slots, every refresh
        /// of the interface threw and the interface stopped updating.
        ///
        /// Dropping the powder from the report leaves the press with what the report means for an
        /// empty machine - it is waiting for product - which is right, because the powder is not
        /// something the press itself can press.
        /// </summary>
        public static void GetMainInputs_Postfix(
            BrickPress __instance,
            ref NativeItemInstance __0,
            ref int __1,
            ref NativeItemInstance __2,
            ref int __3)
        {
            try
            {
                int handled = 0;

                // The product slots can still hold a real product while the feed slots hold
                // powder, so the powder is swapped out rather than the whole report dropped.
                if (IsPowder(__0))
                {
                    handled++;

                    if (IsPowder(__2) || __2 == null)
                    {
                        __0 = null;
                        __1 = 0;
                    }
                    else
                    {
                        __0 = __2;
                        __1 = __3;
                    }
                }

                if (IsPowder(__2))
                {
                    handled++;

                    __2 = null;
                    __3 = 0;
                }

                int id = __instance != null ? __instance.GetInstanceID() : 0;

                if (handled > 0)
                {
                    // The press asks this every frame while its interface is open, so the note is
                    // only worth making once per load.
                    if (PowderHiddenLogged.Add(id))
                    {

                    }
                }
                else if (id != 0)
                {
                    PowderHiddenLogged.Remove(id);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// The task's BEGIN button.
        ///
        /// If this press is holding powder, BEGIN hands the bars over and ends the task on the
        /// spot, which keeps the handle minigame from ever running. A press with no powder in it -
        /// a press loaded with Xanax to be stamped into a brick included - is left completely
        /// alone: the game's own route to a brick runs untouched.
        /// </summary>
        public static bool BeginPress_Prefix(Il2CppScheduleOne.PlayerTasks.UseBrickPress __instance)
        {
            try
            {
                if (__instance == null)
                    return true;

                BrickPress press = __instance.press;

                if (press == null)
                    return true;

                var slots = new List<NativeItemSlot>();
                CollectSlots(press, slots);

                int powder = 0;
                int bars = 0;

                CountWvcGoods(press, out powder, out bars);

                // Only powder makes this a powder press. Xanax bars sitting in the machine are a
                // normal brick run - the player loading the press to have it stamped - and must be
                // left to the game rather than swallowed here.
                if (powder <= 0)
                {


                    return true;
                }



                RunBatch(press, slots);

                StopTask(__instance);

                return false;
            }
            catch (Exception)
            {

                return true;
            }
        }

        /// <summary>
        /// The BEGIN button of the press interface.
        ///
        /// On a press that is holding powder, BEGIN only fills the output slot: the button's own
        /// handler is skipped, so the press task is never created and neither the pour animation
        /// nor the handle minigame can appear. A press with no powder in it - one loaded with
        /// Xanax to be stamped into a brick included - is left exactly as it is.
        /// </summary>
        public static bool BeginButtonPressed_Prefix(
            Il2CppScheduleOne.UI.Stations.BrickPressCanvas __instance)
        {
            try
            {
                BrickPress press = CanvasPress(__instance);

                if (press == null)
                    return true;

                int powder = 0;
                int bars = 0;
                CountWvcGoods(press, out powder, out bars);

                // Powder only: Xanax bars in the machine are a brick run, whose BEGIN belongs to
                // the game.
                if (powder <= 0)
                    return true;



                var slots = new List<NativeItemSlot>();
                CollectSlots(press, slots);

                RunBatch(press, slots);

                RefreshCanvas(__instance);

                return false;
            }
            catch (Exception)
            {


                return true;
            }
        }

        /// <summary>
        /// The point where the interface would start the press task. Skipping it is the hard guard:
        /// without a task there is no pouring step, no pressing step and no minigame. The button
        /// handler above has already filled the output slot by the time this would be reached, and
        /// anything still worth converting is converted here as well.
        ///
        /// Only a press holding powder is stopped here. The task the game is about to start is the
        /// one that presses a product into a brick, so a press loaded with Xanax for bricking -
        /// including the bar the game just picked out of it - is left to run.
        /// </summary>
        public static bool BeginTask_Prefix(
            Il2CppScheduleOne.UI.Stations.BrickPressCanvas __instance,
            NativeProductInstance __0)
        {
            try
            {
                BrickPress press = CanvasPress(__instance);

                if (press == null)
                    return true;

                int powder = 0;
                int bars = 0;
                CountWvcGoods(press, out powder, out bars);

                bool ours = powder > 0 || IsPowder(__0);

                if (!ours)
                    return true;



                var slots = new List<NativeItemSlot>();
                CollectSlots(press, slots);

                RunBatch(press, slots);

                RefreshCanvas(__instance);

                return false;
            }
            catch (Exception)
            {


                return true;
            }
        }

        /// <summary>
        /// Runs while the press interface is open. Powder that was dropped into the interface is
        /// converted the moment it is noticed, so the mould never reaches the pouring or the
        /// pressing step. The game's own check still runs afterwards on the now empty mould, which
        /// leaves the interface open and usable.
        /// </summary>
        public static bool CheckMould_Prefix(
            Il2CppScheduleOne.PlayerTasks.UseBrickPress __instance)
        {
            try
            {
                BrickPress press = __instance?.press;

                if (press == null)
                    return true;

                var slots = new List<NativeItemSlot>();
                CollectSlots(press, slots);

                bool hasPowder = false;

                for (int i = 0; i < slots.Count; i++)
                {
                    if (!IsPowder(slots[i]?.ItemInstance))
                        continue;

                    hasPowder = true;
                    break;
                }

                if (!hasPowder)
                    return true;



                RunBatch(press, slots);

                return true;
            }
            catch (Exception)
            {


                return true;
            }
        }

        /// <summary>The press an interface belongs to, or null when there is none.</summary>
        private static BrickPress CanvasPress(
            Il2CppScheduleOne.UI.Stations.BrickPressCanvas canvas)
        {
            try
            {
                return canvas?.Press;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// True when any of the press's slots holds powder - the same slots a batch is taken from, so
        /// the answer can never disagree with what pressing BEGIN would actually consume.
        ///
        /// Nothing is allocated here on purpose: the press asks about itself every frame while its
        /// interface is open.
        /// </summary>
        private static bool PressHoldsPowder(BrickPress press)
        {
            try
            {
                if (press == null)
                    return false;

                ItemSlot[] mould = press.ProductSlots;

                if (mould != null)
                {
                    for (int i = 0; i < mould.Length; i++)
                    {
                        if (IsPowder(mould[i]?.ItemInstance))
                            return true;
                    }
                }

                var feed = press.InputSlots;

                if (feed != null)
                {
                    for (int i = 0; i < feed.Count; i++)
                    {
                        if (IsPowder(feed[i]?.ItemInstance))
                            return true;
                    }
                }

                var items = press.ItemSlots;

                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (IsPowder(items[i]?.ItemInstance))
                            return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>Counts the powder and the Xanax bars currently inside a press.</summary>
        private static void CountWvcGoods(BrickPress press, out int powder, out int bars)
        {
            powder = 0;
            bars = 0;

            var slots = new List<NativeItemSlot>();
            CollectSlots(press, slots);

            for (int i = 0; i < slots.Count; i++)
            {
                NativeItemInstance item = slots[i]?.ItemInstance;

                if (item == null)
                    continue;

                if (IsPowder(item))
                    powder += item.Quantity;
                else if (IsXanax(item))
                    bars += item.Quantity;
            }
        }

        /// <summary>Redraws the press interface so the output slot shows the fresh bars.</summary>
        private static void RefreshCanvas(
            Il2CppScheduleOne.UI.Stations.BrickPressCanvas canvas)
        {
            try
            {
                if (canvas == null)
                    return;

                BrickPress press = CanvasPress(canvas);

                if (press != null)
                    RefreshVisuals(press);

                _updateInterface?.Invoke(canvas, null);
            }
            catch
            {
            }
        }

        /// <summary>
        /// The whole powder batch, in one go: powder becomes bars, the powder is emptied out of the
        /// machine, and the bars land in the output slot.
        ///
        /// Only powder is taken. Everything else in the press - the product waiting to be stamped
        /// into a brick, and the bars already sitting in the output slot - is left where it is.
        /// </summary>
        private static void RunBatch(BrickPress press, List<NativeItemSlot> slots)
        {
            int units = 0;
            var filled = new List<NativeItemSlot>();

            for (int i = 0; i < slots.Count; i++)
            {
                NativeItemSlot slot = slots[i];
                NativeItemInstance item = slot?.ItemInstance;

                if (item == null || !IsPowder(item))
                    continue;

                units += item.Quantity;
                filled.Add(slot);
            }

            if (units <= 0)
            {

                return;
            }

            // The run takes what the output slot can give back, up to the ceiling: the powder that
            // does not fit waits in the machine for the next press.
            int waiting = BarCount(press);
            int room = OutputRoom(press, waiting);
            int take = units;

            if (take > UnitsPerRun)
                take = UnitsPerRun;

            if (take > room)
                take = room;

            if (take <= 0)
            {


                return;
            }

            int bars = BarsForUnits(take);
            int total = waiting + bars;

            if (!TryCreateItemInstance(Xanax.ProductId, total, out NativeItemInstance output) || output == null)
            {

                return;
            }

            int taken = TakeUnits(filled, take);

            if (taken != take)
            {
                take = taken;

                if (take <= 0)
                    return;

                total = waiting + BarsForUnits(take);

                if (!TryCreateItemInstance(Xanax.ProductId, total, out output) || output == null)
                    return;
            }

            Deliver(press, output, take, total);
        }

        /// <summary>Ends the press task so the interface closes instead of running its minigame.</summary>
        private static void StopTask(Il2CppScheduleOne.PlayerTasks.UseBrickPress task)
        {
            try
            {
                task.StopTask();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// True while this press is in the middle of a powder batch - its mould was found holding
        /// powder and the bars made from it were handed to the press.
        ///
        /// The mark is what tells a press's own run apart from a powder run: only a marked press may
        /// have its finished item unwrapped from the brick the game made of it.
        /// </summary>
        private static bool PowderRunActive(BrickPress press)
        {
            try
            {
                if (press == null)
                    return false;

                float started;

                if (!PowderRuns.TryGetValue(press.GetInstanceID(), out started))
                    return false;

                if (Time.time - started > RunWindow)
                {
                    ClearPowderRun(press);
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int BarCount(BrickPress press)
        {
            try
            {
                NativeItemInstance item = press?.OutputSlot?.ItemInstance;

                return IsXanax(item) ? item.Quantity : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Puts the finished bars in the press's output slot - the same place a finished press
        /// leaves them - and falls back to the player's hands only when there is no output slot.
        /// </summary>
        /// <param name="units">Powder units this run actually consumed.</param>
        /// <param name="total">Bars the output ends up holding, earlier stock included.</param>
        private static void Deliver(BrickPress press, NativeItemInstance bars, int units, int total)
        {
            bool delivered = false;

            try
            {
                NativeItemSlot output = press.OutputSlot;

                if (output != null)
                {
                    output.SetStoredItem(bars, false);
                    output.ReplicateStoredInstance();

                    delivered = true;


                }
                else
                {

                }
            }
            catch (Exception)
            {

            }

            if (!delivered)
            {
                try
                {
                    var inventory = UnityEngine.Object.FindObjectOfType<
                        Il2CppScheduleOne.PlayerScripts.PlayerInventory>();

                    if (inventory != null)
                    {
                        inventory.AddItemToInventory(bars);
                        delivered = true;
                    }
                }
                catch (Exception)
                {

                }
            }

            RefreshVisuals(press);


        }

        /// <summary>
        /// Lets the powder item sit in the press's mould slots, which the base game filters to
        /// products only.
        /// </summary>
        public static bool SlotFilter_Prefix(
            NativeItemSlot __instance,
            NativeItemInstance item,
            ref bool __result)
        {
            try
            {
                if (__instance == null || item == null || !IsPowder(item))
                    return true;

                if (!IsBrickPressSlot(__instance))
                    return true;

                __result = true;
                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Keeps a powder batch loose: the press likes to stamp its brick packaging onto whatever
        /// it just pressed, which is what turned the first run into a single brick.
        /// </summary>
        public static bool SetPackaging_Prefix(
            NativeProductInstance __instance,
            NativePackaging __0)
        {
            try
            {
                if (__instance == null)
                    return true;

                if (!IsXanax(__instance))
                    return true;

                if (!AnyPowderRunActive())
                    return true;

                foreach (KeyValuePair<int, float> run in PowderRuns)
                {
                    if (PackagingSuppressionLogged.Add(run.Key))
                    {

                    }
                }

                return false;
            }
            catch
            {
                return true;
            }
        }


        /// <summary>
        /// Safety net for the mould product itself: if a powder batch somehow reaches the press as
        /// powder (rather than the bars swapped in on BEGIN), it is swapped here before the press
        /// stamps the finished item.
        /// </summary>
        public static bool CompletePress_Prefix(BrickPress __instance, ref NativeProductInstance __0)
        {
            try
            {
                if (__instance == null || __0 == null)
                    return true;

                if (!IsPowder(__0))
                    return true;

                int amount = Math.Max(1, __0.Quantity);

                if (!TryCreateItemInstance(Xanax.ProductId, amount, out NativeItemInstance bars) || bars == null)
                    return true;

                NativeProductInstance replacement = bars.TryCast<NativeProductInstance>();

                if (replacement == null)
                    return true;

                MarkPowderRun(__instance);



                __0 = replacement;

                return true;
            }
            catch (Exception)
            {

                return true;
            }
        }

        /// <summary>
        /// Logs what the press actually produced and, when the press was in the middle of a powder
        /// batch, makes sure the output is the loose bars the player asked for rather than a brick.
        ///
        /// A press that was not mid batch is the game's own run: the brick it just stamped out of
        /// the player's product is the player's, and is never unwrapped.
        /// </summary>
        public static void CompletePress_Postfix(BrickPress __instance, NativeProductInstance __0)
        {
            try
            {
                if (__instance == null)
                    return;



                if (!PowderRunActive(__instance))
                {


                    return;
                }

                MelonCoroutines.Start(CheckOutput(__instance));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Waits for the press to finish moving the item into the output slot, then unwraps a brick
        /// back into the loose bars it was pressed from.
        /// </summary>
        private static IEnumerator CheckOutput(BrickPress press)
        {
            yield return new WaitForSeconds(1.0f);

            try
            {
                NativeItemSlot output = press?.OutputSlot;

                if (output == null)
                {

                    yield break;
                }

                NativeItemInstance stored = output.ItemInstance;

                if (stored == null)
                {

                    ClearPowderRun(press);
                    yield break;
                }



                if (IsXanax(stored) && string.Equals(GetPackagingId(stored), BrickPackagingId, StringComparison.OrdinalIgnoreCase))
                {
                    if (TryCreateItemInstance(Xanax.ProductId, stored.Quantity, out NativeItemInstance bars) && bars != null)
                    {
                        output.SetStoredItem(bars, true);

                        try
                        {
                            output.ReplicateStoredInstance();
                        }
                        catch
                        {
                        }

                        RefreshVisuals(press);


                    }
                }

                ClearPowderRun(press);
            }
            catch (Exception)
            {

            }
        }


        private static void MarkPowderRun(BrickPress press)
        {
            try
            {
                if (press == null)
                    return;

                PowderRuns[press.GetInstanceID()] = Time.time;
            }
            catch
            {
            }
        }

        private static void ClearPowderRun(BrickPress press)
        {
            try
            {
                if (press == null)
                    return;

                int id = press.GetInstanceID();

                PowderRuns.Remove(id);
                PackagingSuppressionLogged.Remove(id);
            }
            catch
            {
            }
        }

        private static bool AnyPowderRunActive()
        {
            try
            {
                if (PowderRuns.Count == 0)
                    return false;

                List<int> expired = null;

                foreach (KeyValuePair<int, float> run in PowderRuns)
                {
                    if (Time.time - run.Value > RunWindow)
                    {
                        expired ??= new List<int>();
                        expired.Add(run.Key);
                    }
                }

                if (expired != null)
                {
                    foreach (int id in expired)
                    {
                        PowderRuns.Remove(id);
                        PackagingSuppressionLogged.Remove(id);
                    }
                }

                return PowderRuns.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPowder(NativeItemInstance item)
        {
            return IdEquals(item?.Definition?.ID, XanaxPowder.ItemId);
        }

        private static bool IsXanax(NativeItemInstance item)
        {
            return IdEquals(item?.Definition?.ID, Xanax.ProductId);
        }

        private static bool IsBrickPressSlot(NativeItemSlot slot)
        {
            try
            {
                if (slot?.SlotOwner == null)
                    return false;

                Il2CppObjectBase owner = slot.SlotOwner as Il2CppObjectBase;

                return owner?.TryCast<BrickPress>() != null;
            }
            catch
            {
                return false;
            }
        }

        private static string GetPackagingId(NativeItemInstance item)
        {
            try
            {
                NativeProductInstance product = item?.TryCast<NativeProductInstance>();

                if (product == null)
                    return null;

                return product.PackagingID ?? product.packaging?.ID;
            }
            catch
            {
                return null;
            }
        }


        private static void RefreshVisuals(BrickPress press)
        {
            try
            {
                _updateInputVisuals?.Invoke(press, null);
            }
            catch
            {
            }
        }

        private static bool TryCreateItemInstance(
            string itemId,
            int quantity,
            out NativeItemInstance instance)
        {
            instance = null;

            try
            {
                S1API.Items.ItemDefinition wrapper = S1API.Items.ItemManager.GetDefinition(itemId);

                if (wrapper == null)
                {

                    return false;
                }

                object raw = GetMemberValue(wrapper, "S1ItemDefinition");

                NativeStorableDefinition definition = raw as NativeStorableDefinition;

                if (definition == null)
                {

                    return false;
                }

                instance = definition.GetDefaultInstance(Math.Max(1, quantity));

                return instance != null;
            }
            catch (Exception)
            {

                return false;
            }
        }

        private static bool IdEquals(string left, string right)
        {
            return !string.IsNullOrEmpty(left) &&
                   !string.IsNullOrEmpty(right) &&
                   string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static object GetMemberValue(object target, string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);

                    if (property != null)
                    {
                        try
                        {
                            return property.GetValue(target);
                        }
                        catch
                        {
                        }
                    }

                    FieldInfo field = type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);

                    if (field != null)
                    {
                        try
                        {
                            return field.GetValue(target);
                        }
                        catch
                        {
                        }
                    }

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return null;
        }

    }
}
