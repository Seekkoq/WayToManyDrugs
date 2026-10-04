using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public enum OffshoreState
    {
        Idle,
        AwaitingProduct,
        PreparingDeparture,
        VoyageActive,
        ReturnedWithCash
    }

    public struct OffshoreOrderInfo
    {
        public string DrugDisplayName;
        public string PrimaryItemId;
        public string SecondaryItemId;
        public int RequiredQuantity;
        public float PayoutPerUnit;
        public string Destination;
        public string BuyerSyndicate;
        public float TotalPayout => RequiredQuantity * PayoutPerUnit;
    }

    public static class OffshoreSalesManager
    {
        public static Vector3 DockSpawnPosition = new Vector3(-84.807f, -2.36f, -26.24f);
        public static Quaternion DockSpawnRotation = Quaternion.Euler(0f, 116.95f, 0f);

        public static readonly Vector3 OverseasVoyagePosition = new Vector3(-84.807f, -500f, -26.24f);

        private static readonly OffshoreOrderInfo[] DrugCatalog = new OffshoreOrderInfo[]
        {
            new OffshoreOrderInfo
            {
                DrugDisplayName = "MDMA",
                PrimaryItemId = "westvilleconnection:products/mdma_v3",
                SecondaryItemId = "mdma",
                RequiredQuantity = 15,
                PayoutPerUnit = 240f
            },
            new OffshoreOrderInfo
            {
                DrugDisplayName = "Cocaine",
                PrimaryItemId = "cocaine",
                SecondaryItemId = "coke",
                RequiredQuantity = 15,
                PayoutPerUnit = 220f
            },
            new OffshoreOrderInfo
            {
                DrugDisplayName = "DMT",
                PrimaryItemId = "westvilleconnection:products/dmt",
                SecondaryItemId = "dmt",
                RequiredQuantity = 10,
                PayoutPerUnit = 350f
            },
            new OffshoreOrderInfo
            {
                DrugDisplayName = "Salvia",
                PrimaryItemId = "westvilleconnection:products/salvia_v1",
                SecondaryItemId = "salvia",
                RequiredQuantity = 15,
                PayoutPerUnit = 190f
            },
            new OffshoreOrderInfo
            {
                DrugDisplayName = "THC Gummies",
                PrimaryItemId = "westvilleconnection:products/thc_gummies",
                SecondaryItemId = "thc_gummies",
                RequiredQuantity = 20,
                PayoutPerUnit = 130f
            }
        };

        private static readonly (string City, string Country, string Syndicate)[] Destinations = new[]
        {
            ("Rotterdam", "Netherlands", "Euro-Continental Syndicate"),
            ("Tokyo", "Japan", "Yokohama Cartel"),
            ("Liverpool", "United Kingdom", "Merseyside Maritime Network"),
            ("Marseille", "France", "Mediterranean Syndicate"),
            ("Hamburg", "Germany", "North Sea Smuggling Ring"),
            ("Santos", "Brazil", "South American Consortium"),
            ("Sydney", "Australia", "Pacific Rim Buyers"),
            ("Antwerp", "Belgium", "Flanders Syndicate"),
            ("Singapore", "Singapore", "Straits Consortium"),
            ("Valparaíso", "Chile", "Andes Coast Syndicate"),
            ("Naples", "Italy", "Campania Cargo Ring"),
            ("Busan", "South Korea", "Korean Strait Collective"),
            ("Cape Town", "South Africa", "Cape Horn Traders"),
            ("Dubai", "UAE", "Gulf Coast Brotherhood"),
            ("Manila", "Philippines", "Luzon Maritime Circle"),
            ("Reykjavík", "Iceland", "North Atlantic Circle"),
            ("Casablanca", "Morocco", "Straits of Gibraltar Syndicate"),
            ("Panama City", "Panama", "Canal Zone Brokers"),
            ("Odessa", "Ukraine", "Black Sea Network"),
            ("Haiphong", "Vietnam", "Tonkin Gulf Syndicate")
        };

        private static readonly string[] OrderMessageTemplates = new[]
        {
            "I've got an overseas client in {destination} in desperate need of {quantity}x {drug}. Meet me at the docks with the cargo before the tide rolls out.",
            "A high-profile contact from {syndicate} ({destination}) placed an urgent order for {quantity}x {drug}. Bring the product to my boat at the docks.",
            "Word from overseas just lit up: {syndicate} in {destination} is ready to pay top dollar for {quantity}x {drug}. I'm docked waiting for the shipment.",
            "The sea is calm and the freight route to {destination} is clear. I need {quantity}x {drug} loaded up ASAP. Meet me down at the pier.",
            "Got international buyers in {destination} waiting on a {quantity}x {drug} run. Meet me at the dock before maritime patrol changes shifts.",
            "You won't believe what {syndicate} in {destination} just wired me for. {quantity}x {drug}, sealed and sea-ready. Get down to the pier.",
            "Tide's turning in an hour and my phone hasn't stopped. {destination} wants {quantity}x {drug}. You in?",
            "Coffee's cold, paperwork's done, and the buyers in {destination} are champing at the bit for {quantity}x {drug}. Come see me at the docks.",
            "Just got off the shortwave with {destination}. They're short on supply and long on cash. {quantity}x {drug} - bring it to my slip.",
            "It's a long haul to {destination} but the {syndicate} pays better than anyone. I need {quantity}x {drug} before the harbor master does his rounds.",
            "My contact in {destination} says the streets are dry. {quantity}x {drug} and we both eat good this month. Down at the boat.",
            "The Night Wave is fueled and itching to leave. {destination} client, {quantity}x {drug}. You bring it, I float it."
        };

        private static readonly string[] ReturnMessageTemplates = new[]
        {
            "I'm back from out of town. The overseas buyers in {destination} paid top dollar for the {drug}. Meet me at the docks to collect your cut.",
            "Just tied off at the pier. Smooth sailing all the way from {destination}. Your cut of the cash is counted and ready at the docks.",
            "We made port safe and sound. The {syndicate} in {destination} took the entire {drug} shipment without a hitch. Come grab your money.",
            "Declan here. Dropped anchor at the dock with clean bills from the overseas run. Meet me by the slip to settle up.",
            "The voyage to {destination} was a complete success. Your cash is with me down at the docks. Come get what's yours before I cast off again.",
            "Salt in my beard and cash in the hold. {destination} was good to us. Come down to the pier for your share.",
            "That run to {destination} went smoother than a calm sea. Your cut's waiting at the docks - don't make me sit on it all night.",
            "Engines cooled, books open. {destination} paid out clean for the {drug}. Your share is sitting with me at the pier.",
            "Back at the slip. Not a single coast guard wake the whole way back from {destination}. Come collect your money.",
            "The {syndicate} client wouldn't stop raving about the product. We're square - your cash is at the docks."
        };

        public static OffshoreState State { get; private set; } = OffshoreState.Idle;
        public static OffshoreOrderInfo CurrentOrder { get; private set; }
        public static Il2CppScheduleOne.ItemFramework.EQuality LastHandoverQuality = Il2CppScheduleOne.ItemFramework.EQuality.Standard;

        private static float _orderCooldownTimer = 0f;
        private const float MinOrderCooldown = 1200f;
        private static float _departureTimer = 0f;
        private const float DepartureDelay = 30f;
        private static float _voyageTimer = 0f;
        private const float VoyageDuration = 180f;

        private static readonly System.Random Rng = new System.Random();

        private static float _phoneSendableTimer = 0f;

        /// <summary>Chance Declan takes a run the player asks for on the spot, from the phone.</summary>
        private const float RunNowAcceptChance = 0.7f;

        /// <summary>
        /// How long Declan makes the player wait after he turns a run request down. Counted in real
        /// seconds off the frame clock, the same clock the other offshore timers use.
        /// </summary>
        private const float RunNowRefusalCooldown = 300f;

        /// <summary>The phone reply that asks Declan to line a run up there and then.</summary>
        private const string RunNowSendableText = "Wanna run one up right now?";

        private static float _runNowRefusalTimer = 0f;

        private const int MaxNegotiationAttempts = 2;
        private static int _negotiationAttempts = 0;
        private static bool _orderNegotiated = false;

        public static void TryNegotiateQuantity(float fraction)
        {
            if (State != OffshoreState.AwaitingProduct)
            {
                SendDeclanMessage("Declan: Nothing to negotiate right now. No shipment is on the table.");
                return;
            }

            if (_orderNegotiated)
            {
                SendDeclanMessage($"Declan: We already shook hands on {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName}. The buyers won't budge twice. Bring the cargo.");
                return;
            }

            if (_negotiationAttempts >= MaxNegotiationAttempts)
            {
                SendDeclanMessage($"Declan: I'm done haggling. The manifest says {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName} and that's final. Deliver it or lose the deal.");
                return;
            }

            _negotiationAttempts++;

            float acceptChance = fraction switch
            {
                >= 0.9f => 0.30f,
                >= 0.75f => 0.20f,
                >= 0.5f => 0.08f,
                _ => 0.02f
            };

            if (Rng.NextDouble() < acceptChance)
            {
                int newQuantity = Math.Max(5, (int)MathF.Round(CurrentOrder.RequiredQuantity * fraction / 5f) * 5);
                newQuantity = Math.Min(newQuantity, CurrentOrder.RequiredQuantity);
                int oldQuantity = CurrentOrder.RequiredQuantity;
                var order = CurrentOrder;
                order.RequiredQuantity = newQuantity;
                CurrentOrder = order;
                _orderNegotiated = true;


                string[] acceptReplies = new[]
                {
                    $"Declan: Alright, deal. I'll tell the buyers in {CurrentOrder.Destination} to expect {newQuantity}x {CurrentOrder.DrugDisplayName} instead of {oldQuantity}. Your cut drops to ${CurrentOrder.TotalPayout:F0}. Don't push your luck again.",
                    $"Declan: *sighs* Fine. {newQuantity}x {CurrentOrder.DrugDisplayName} it is. Payout's now ${CurrentOrder.TotalPayout:F0}. Get it down to the docks.",
                    $"Declan: The syndicate grumbled, but they'll take {newQuantity}x {CurrentOrder.DrugDisplayName}. New total for you: ${CurrentOrder.TotalPayout:F0}. Bring the cargo."
                };
                SendDeclanMessage(acceptReplies[Rng.Next(acceptReplies.Length)]);
            }
            else
            {
                if (_negotiationAttempts >= MaxNegotiationAttempts)
                {
                    CancelOrder("Declan: That's it, I'm done with this. The buyers in {destination} won't take less than what's on the manifest, and you've wasted my time twice now. The deal is OFF. I'll text you if I ever feel like working with you again.");
                }
                else
                {
                    string[] rejectReplies = new[]
                    {
                        $"Declan: No chance. The client in {CurrentOrder.Destination} ordered {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName}. Short-changing them costs me my reputation.",
                        $"Declan: You're joking, right? The manifest says {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName}. One more attempt to lowball me and the deal's off."
                    };
                    SendDeclanMessage(rejectReplies[Rng.Next(rejectReplies.Length)]);
                }
            }
        }

        public static void OnPlayerOffered90Percent() => TryNegotiateQuantity(0.9f);
        public static void OnPlayerOffered75Percent() => TryNegotiateQuantity(0.75f);
        public static void OnPlayerOffered50Percent() => TryNegotiateQuantity(0.5f);

        public static void CancelOrder(string messageTemplate)
        {
            string message = (messageTemplate ?? "Declan: The deal is off.")
                .Replace("{destination}", CurrentOrder.Destination)
                .Replace("{syndicate}", CurrentOrder.BuyerSyndicate)
                .Replace("{quantity}", CurrentOrder.RequiredQuantity.ToString())
                .Replace("{drug}", CurrentOrder.DrugDisplayName);

            State = OffshoreState.Idle;
            _orderCooldownTimer = 0f;
            _negotiationAttempts = 0;
            _orderNegotiated = false;
            CurrentOrder = default;
            CaptainDeclanCross.ResumeSchedule();

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }

            SendDeclanMessage(message);
        }

        public static void Update()
        {
            if (_runNowRefusalTimer > 0f)
                _runNowRefusalTimer = Mathf.Max(0f, _runNowRefusalTimer - Time.deltaTime);

            _phoneSendableTimer += Time.deltaTime;
            if (_phoneSendableTimer >= 3.0f)
            {
                _phoneSendableTimer = 0f;
                if (CaptainDeclanCross.Instance != null)
                {
                    ConfigurePhoneSendables();
                }
            }

            switch (State)
            {
                case OffshoreState.Idle:
                    _orderCooldownTimer += Time.deltaTime;
                    if (_orderCooldownTimer >= MinOrderCooldown * OffshoreHeat.CooldownMultiplier)
                    {
                        _orderCooldownTimer = 0f;
                        if (OffshoreHeat.CanSail)
                        {
                            TriggerOffshoreSale();
                        }
                    }
                    break;

                case OffshoreState.AwaitingProduct:
                    CaptainDeclanCross.WalkToDock();
                    break;

                case OffshoreState.PreparingDeparture:
                    _departureTimer += Time.deltaTime;
                    CaptainDeclanCross.WalkToDock();

                    if (_departureTimer >= DepartureDelay)
                    {
                        _departureTimer = 0f;
                        State = OffshoreState.VoyageActive;
                        _voyageTimer = 0f;

                        CaptainDeclanCross.TryWarp(OverseasVoyagePosition, Quaternion.identity);

                        if (CaptainDeclanCross.Instance != null)
                        {
                            OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
                        }
                    }
                    break;

                case OffshoreState.VoyageActive:
                    _voyageTimer += Time.deltaTime;
                    if (_voyageTimer >= VoyageDuration)
                    {
                        CompleteVoyageAndReturn();
                    }
                    break;

                case OffshoreState.ReturnedWithCash:
                    CaptainDeclanCross.WalkToDock();
                    break;
            }
        }

        public static bool TriggerOffshoreSale(bool manual = false)
        {
            if (State != OffshoreState.Idle)
            {
                return false;
            }

            if (!OffshoreHeat.CanSail)
            {
                SendDeclanMessage(OffshoreHeat.GetRefusalMessage());
                return false;
            }

            if (manual)
            {
                if (OffshoreHeat.IntroDealUsed && !OffshoreHeat.IsShotCaller)
                {
                    string[] gates = new[]
                    {
                        "Declan: Easy, sailor. I only run overseas cargo for names that carry weight now. Make Shot Caller and I'll text you the moment the lane opens up.",
                        "Declan: Not yet, friend. You're not big enough for the overseas buyers. Hit Shot Caller and my phone will find yours.",
                        "Declan: The syndicates don't take calls from small names. Get to Shot Caller first - then we'll talk about the overseas run."
                    };
                    SendDeclanMessage(gates[Rng.Next(gates.Length)]);
                    return false;
                }
            }
            else if (!OffshoreHeat.IsShotCaller)
            {
                return false;
            }

            int index = Rng.Next(DrugCatalog.Length);
            var order = DrugCatalog[index];

            switch (order.DrugDisplayName.ToLowerInvariant())
            {
                case "mdma":
                    order.RequiredQuantity = Rng.Next(5, 9) * 5;
                    break;
                case "cocaine":
                    order.RequiredQuantity = Rng.Next(5, 8) * 5;
                    break;
                case "dmt":
                    order.RequiredQuantity = Rng.Next(5, 7) * 5;
                    break;
                case "salvia":
                    order.RequiredQuantity = Rng.Next(5, 9) * 5;
                    break;
                case "thc gummies":
                    order.RequiredQuantity = Rng.Next(5, 11) * 5;
                    break;
                default:
                    order.RequiredQuantity = Rng.Next(5, 10) * 5;
                    break;
            }

            var dest = Destinations[Rng.Next(Destinations.Length)];

            order.Destination =
                string.Equals(dest.City, dest.Country, StringComparison.OrdinalIgnoreCase)
                    ? dest.City
                    : $"{dest.City}, {dest.Country}";

            order.BuyerSyndicate = dest.Syndicate;
            CurrentOrder = order;

            _negotiationAttempts = 0;
            _orderNegotiated = false;

            State = OffshoreState.AwaitingProduct;

            OffshoreHeat.MarkIntroDealUsed();

            CaptainDeclanCross.SuspendSchedule();
            CaptainDeclanCross.WalkToDock();

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }

            string template = OrderMessageTemplates[Rng.Next(OrderMessageTemplates.Length)];
            var message = new System.Text.StringBuilder(template
                .Replace("{destination}", CurrentOrder.Destination)
                .Replace("{syndicate}", CurrentOrder.BuyerSyndicate)
                .Replace("{quantity}", CurrentOrder.RequiredQuantity.ToString())
                .Replace("{drug}", CurrentOrder.DrugDisplayName));

            string heatFlavor = OffshoreHeat.GetOrderFlavorLine();
            if (!string.IsNullOrEmpty(heatFlavor))
            {
                message.Append(' ').Append(heatFlavor);
            }

            try
            {
                var captain = CaptainDeclanCross.Instance;
                if (captain != null && captain.gameObject != null)
                {
                    float walkDist = Vector3.Distance(
                        captain.gameObject.transform.position,
                        DockSpawnPosition
                    );

                    if (walkDist > 15f)
                    {
                        string[] walkNotices = new[]
                        {
                            "Give me a few minutes to get down to the pier - I'm coming on foot.",
                            "I'm making my way to the docks now. It's a short walk - give me a minute or two.",
                            "Don't leave just yet. I still have to walk over to the boat - a couple of minutes at most.",
                            "I'm headed down to the pier right now. Give me a few minutes to get there.",
                            "On my way to the slip. It'll take me a couple of minutes on foot."
                        };

                        message.Append(' ').Append(walkNotices[Rng.Next(walkNotices.Length)]);
                    }
                }
            }
            catch
            {
            }

            SendDeclanMessage(message.ToString());
            return true;
        }

        public static bool PlayerSubmitProduct()
        {
            if (State != OffshoreState.AwaitingProduct)
                return false;

            int count = CountPlayerDrug(CurrentOrder);
            if (count < CurrentOrder.RequiredQuantity)
            {
                return false;
            }

            bool removed = DeductPlayerDrug(
                CurrentOrder,
                CurrentOrder.RequiredQuantity,
                out int removedUnits,
                out bool tookExtraPackage,
                out int keptSampleUnits);

            if (!removed)
            {

                SendDeclanMessage("Declan: I can't load the hold from what you're carrying. The manifest has to be met in sealed packages - nothing loose, nothing I can't stack. Repackage it and come back.");
                return false;
            }


            if (tookExtraPackage)
            {
                SendDeclanMessage($"Declan: The manifest says {CurrentOrder.RequiredQuantity}, but your sealed packages don't split that clean. I took the extra full package to make the load - the surplus sails with us as ballast.");
            }

            if (keptSampleUnits > 0)
            {
                SendDeclanMessage($"Declan: One of your sealed packages was stamped like customs already inspected it - wouldn't budge. I left it with you; the rest of the {CurrentOrder.DrugDisplayName} is on board.");
            }

            State = OffshoreState.PreparingDeparture;
            _departureTimer = 0f;

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }

            string qualityRemark;
            switch (LastHandoverQuality)
            {
                case Il2CppScheduleOne.ItemFramework.EQuality.Trash:
                case Il2CppScheduleOne.ItemFramework.EQuality.Poor:
                    qualityRemark = $"Batch is pretty rough around the edges, but the buyers in {CurrentOrder.Destination} will take what they can get.";
                    break;
                case Il2CppScheduleOne.ItemFramework.EQuality.Premium:
                    qualityRemark = $"Cargo looks crisp and potent. The syndicate in {CurrentOrder.Destination} will be very pleased with this batch.";
                    break;
                case Il2CppScheduleOne.ItemFramework.EQuality.Heavenly:
                    qualityRemark = $"Exceptional purity on this product. Rare to see work this clean. The overseas buyers will pay top dollar.";
                    break;
                case Il2CppScheduleOne.ItemFramework.EQuality.Standard:
                default:
                    qualityRemark = $"Standard grade cut. Clean enough to pass maritime inspection without hassle.";
                    break;
            }

            SendDeclanMessage($"Cargo received. {qualityRemark} Securing the hold and casting off for {CurrentOrder.Destination} in 30 seconds. Keep your phone handy.");
            return true;
        }

        public static void CompleteVoyageAndReturn(bool forceSeizure = false)
        {
            if (State != OffshoreState.VoyageActive)
                return;

            _voyageTimer = 0f;

            bool seized = forceSeizure || Rng.NextDouble() < OffshoreHeat.CurrentSeizureChance;
            if (seized)
            {
                SeizeShipment();
                return;
            }

            State = OffshoreState.ReturnedWithCash;

            CaptainDeclanCross.TryWarp(DockSpawnPosition, DockSpawnRotation);

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }


            string template = ReturnMessageTemplates[Rng.Next(ReturnMessageTemplates.Length)];
            string returnMessage = template
                .Replace("{destination}", CurrentOrder.Destination)
                .Replace("{syndicate}", CurrentOrder.BuyerSyndicate)
                .Replace("{quantity}", CurrentOrder.RequiredQuantity.ToString())
                .Replace("{drug}", CurrentOrder.DrugDisplayName);

            SendDeclanMessage(returnMessage);
        }

        private static void SeizeShipment()
        {
            State = OffshoreState.Idle;
            _orderCooldownTimer = 0f;
            _negotiationAttempts = 0;
            _orderNegotiated = false;

            CaptainDeclanCross.TryWarp(DockSpawnPosition, DockSpawnRotation);
            CaptainDeclanCross.ResumeSchedule();

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }


            string[] seizureMessages = new[]
            {
                "Declan: Disaster. Coast guard cutters boxed us in just outside the shipping lane and seized the whole load - {quantity}x {drug}. The buyers in {destination} got nothing, and neither did we. No payout this run.",
                "Declan: Mayday, mayday... Customs flagged the container in {destination} and impounded the entire {quantity}x {drug} shipment. They burned my paperwork too. We're out the cargo and the cash. I'm sorry, kid.",
                "Declan: The deal went bad. Port authority raided the handover in {destination} - they took all {quantity}x {drug} and my phone has been blowing up with angry calls. The {syndicate} is furious. No payout. We'll get them back next run.",
                "Declan: I barely made it out. An interdiction plane spotted us mid-crossing; I had to dump the {quantity}x {drug} overboard to dodge the bust. The buyers got nothing. No payout this time. I owe you one.",
                "Declan: We got hit. A naval patrol pulled us over in international waters and found the hidden compartments. They confiscated all {quantity}x {drug} and fined me on the spot. The {destination} run is a total loss - no payout.",
                "Declan: Black news. The buyers' rivals tipped off the harbor police in {destination}. They seized the {quantity}x {drug} the moment we docked. I got out with my skin and nothing else. No payout this run.",
                "Declan: Storm warning came too late. A coast guard plane forced us back to port and the whole {quantity}x {drug} consignment went into an evidence locker. The {syndicate} is going to be livid. No payout.",
                "Declan: The shipment's gone. Inspectors in {destination} X-rayed the crates and pulled all {quantity}x {drug}. I burned the false papers and ran. Forget this run ever happened - I'll make it up to you."
            };

            string message = seizureMessages[Rng.Next(seizureMessages.Length)]
                .Replace("{destination}", CurrentOrder.Destination)
                .Replace("{syndicate}", CurrentOrder.BuyerSyndicate)
                .Replace("{quantity}", CurrentOrder.RequiredQuantity.ToString())
                .Replace("{drug}", CurrentOrder.DrugDisplayName);

            SendDeclanMessage(message);

            OffshoreHeat.RegisterSeizure();
        }

        public static bool PlayerCollectPayout()
        {
            if (State != OffshoreState.ReturnedWithCash)
                return false;

            float payout = CurrentOrder.TotalPayout;

            float heatBonus = OffshoreHeat.PayoutBonus;
            if (heatBonus > 0f)
            {
                float bonusAmount = payout * heatBonus;
                payout += bonusAmount;
                SendDeclanMessage($"Declan: And there's your danger premium on top - an extra ${bonusAmount:F0} for sailing hot waters. You earned every bill of it.");
            }


            AwardCash(payout);

            OffshoreHeat.RegisterSuccessfulRun(CurrentOrder.RequiredQuantity);

            State = OffshoreState.Idle;
            _orderCooldownTimer = 0f;
            _negotiationAttempts = 0;
            _orderNegotiated = false;
            CaptainDeclanCross.ResumeSchedule();

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }


            string[] payoutFollowUps = new[]
            {
                "Pleasure doing business. Whenever I get another deal lined up, I'll let you know.",
                "Good doing business with you. Whenever another buyer reaches out from across the waters, I'll send you a message.",
                "Cash is settled. I'll keep my phone on and let you know whenever the next shipment comes in.",
                "All squared away. Whenever I get another run ready, I'll text you."
            };
            SendDeclanMessage(payoutFollowUps[Rng.Next(payoutFollowUps.Length)]);
            return true;
        }

        public static void ConfigurePhoneSendables()
        {
            try
            {
                var declan = CaptainDeclanCross.Instance;
                if (declan == null || declan.gameObject == null)
                    return;

                var npc = declan.gameObject.GetComponent<Il2CppScheduleOne.NPCs.NPC>()
                       ?? declan.gameObject.GetComponentInChildren<Il2CppScheduleOne.NPCs.NPC>(true);

                if (npc == null || npc.MSGConversation == null)
                    return;

                var conv = npc.MSGConversation;

                if (conv.Sendables == null)
                    conv.Sendables = new Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Messaging.SendableMessage>();

                bool hasStatus = false;
                bool hasDetails = false;
                bool hasOffer90 = false;
                bool hasOffer75 = false;
                bool hasDecline = false;
                bool hasRunNow = false;

                for (int i = 0; i < conv.Sendables.Count; i++)
                {
                    var s = conv.Sendables[i];
                    if (s != null)
                    {
                        if (s.Text == "Shipment status?")
                            hasStatus = true;
                        else if (s.Text == "Shipment details?")
                            hasDetails = true;
                        else if (s.Text == "Offer 90%")
                            hasOffer90 = true;
                        else if (s.Text == "Offer 75%")
                            hasOffer75 = true;
                        else if (s.Text == "Decline this deal")
                            hasDecline = true;
                        else if (s.Text == RunNowSendableText)
                            hasRunNow = true;
                    }
                }

                if (!hasStatus)
                {
                    var statusSendable = conv.CreateSendableMessage("Shipment status?");
                    if (statusSendable != null)
                    {
                        statusSendable.onSent = new Action(OnPlayerAskedStatus);
                    }
                }

                if (!hasDetails)
                {
                    var detailsSendable = conv.CreateSendableMessage("Shipment details?");
                    if (detailsSendable != null)
                    {
                        detailsSendable.onSent = new Action(OnPlayerAskedDetails);
                    }
                }

                if (!hasOffer90)
                {
                    var offer90 = conv.CreateSendableMessage("Offer 90%");
                    if (offer90 != null)
                    {
                        offer90.onSent = new Action(OnPlayerOffered90Percent);
                    }
                }

                if (!hasOffer75)
                {
                    var offer75 = conv.CreateSendableMessage("Offer 75%");
                    if (offer75 != null)
                    {
                        offer75.onSent = new Action(OnPlayerOffered75Percent);
                    }
                }

                if (!hasDecline)
                {
                    var declineSendable = conv.CreateSendableMessage("Decline this deal");
                    if (declineSendable != null)
                    {
                        declineSendable.onSent = new Action(OnPlayerDeclinedDeal);
                    }
                }

                if (!hasRunNow)
                {
                    var runNowSendable = conv.CreateSendableMessage(RunNowSendableText);
                    if (runNowSendable != null)
                    {
                        runNowSendable.onSent = new Action(OnPlayerRequestedRunNow);
                    }
                }

                if (conv.senderInterface != null)
                {
                    conv.senderInterface.UpdateSendables();
                }
            }
            catch (Exception)
            {

            }
        }

        public static void OnPlayerAskedStatus()
        {
            string reply;
            switch (State)
            {
                case OffshoreState.Idle:
                    string[] idleReplies = new[]
                    {
                        "Declan: All quiet on the overseas line right now. No overseas shipments active. I'll text you when international clients reach out.",
                        "Declan: Waters are calm and the deck is clear. No active runs right now. I'll hit you up when a client needs product.",
                        "Declan: Just routine harbor maintenance right now. When the next buyer from overseas calls in an order, you'll be the first to know."
                    };
                    reply = idleReplies[Rng.Next(idleReplies.Length)];
                    break;

                case OffshoreState.AwaitingProduct:
                    string[] awaitingReplies = new[]
                    {
                        $"Declan: Currently tied up at the docks waiting on the {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName} shipment for our client in {CurrentOrder.Destination}.",
                        $"Declan: Boat is ready to cast off as soon as you bring down the {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName}. I'm down at the docks.",
                        $"Declan: Standing by at the slip. Bring the {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName} over so we can get moving to {CurrentOrder.Destination}."
                    };
                    reply = awaitingReplies[Rng.Next(awaitingReplies.Length)];
                    break;

                case OffshoreState.PreparingDeparture:
                    string[] prepReplies = new[]
                    {
                        $"Declan: Crates are in the hold. Securing the hatches and warming the engines. Casting off for {CurrentOrder.Destination} in a few moments.",
                        $"Declan: Lashing down the cargo and checking the rudder. We set sail for {CurrentOrder.Destination} in under thirty seconds.",
                        $"Declan: Just finishing the engine check. Heading out into open waters for {CurrentOrder.Destination} right now."
                    };
                    reply = prepReplies[Rng.Next(prepReplies.Length)];
                    break;

                case OffshoreState.VoyageActive:
                    int minutesLeft = Mathf.CeilToInt(Mathf.Max(1f, (VoyageDuration - _voyageTimer) / 60f));
                    string[] voyageReplies = new[]
                    {
                        $"Declan: Making good knots across international shipping lanes toward {CurrentOrder.Destination}. Cargo is secured in the hold. ETA roughly {minutesLeft} minutes.",
                        $"Declan: Heavy seas but the engine is purring. The {CurrentOrder.DrugDisplayName} is safe below deck. Should make port in {CurrentOrder.Destination} soon.",
                        $"Declan: Out in international waters on the freight run to {CurrentOrder.Destination}. Everything is on schedule. ETA {minutesLeft} minutes."
                    };
                    reply = voyageReplies[Rng.Next(voyageReplies.Length)];
                    break;

                case OffshoreState.ReturnedWithCash:
                    string[] returnedReplies = new[]
                    {
                        $"Declan: We made port at the docks! I have your ${CurrentOrder.TotalPayout:F0} cut from the {CurrentOrder.Destination} run right here with me. Come meet me at the pier.",
                        $"Declan: Tied off at the slip with your cut. ${CurrentOrder.TotalPayout:F0} in clean bills. Come down to the docks to collect.",
                        $"Declan: Back in town with the cash from {CurrentOrder.Destination}. Meet me at the docks so we can settle the books."
                    };
                    reply = returnedReplies[Rng.Next(returnedReplies.Length)];
                    break;

                default:
                    reply = "Declan: Overseas line is quiet.";
                    break;
            }

            reply += " " + OffshoreHeat.GetStatusLine();

            SendDeclanMessage(reply);
        }

        public static void OnPlayerAskedDetails()
        {
            string reply;
            if (State == OffshoreState.Idle)
            {
                string[] idleDetails = new[]
                {
                    "Declan: No active manifest on the books right now. When an overseas shipment order comes up, I'll send you the full cargo details.",
                    "Declan: No contracts active at the moment. As soon as a foreign buyer puts in a request, I'll transmit the cargo manifest."
                };
                reply = idleDetails[Rng.Next(idleDetails.Length)];
            }
            else
            {
                reply = $"Declan: [Shipment Manifest]\n" +
                        $"• Cargo: {CurrentOrder.RequiredQuantity}x {CurrentOrder.DrugDisplayName}\n" +
                        $"• Port of Call: {CurrentOrder.Destination}\n" +
                        $"• Buyer: {CurrentOrder.BuyerSyndicate}\n" +
                        $"• Payout: ${CurrentOrder.TotalPayout:F0} (${CurrentOrder.PayoutPerUnit:F0}/unit)\n" +
                        $"• Vessel: M/V Night Wave (Offshore trawler)\n" +
                        $"• Lane Heat: {OffshoreHeat.TierName} ({OffshoreHeat.Heat:F0}/100)\n" +
                        $"• Interdiction Risk: {OffshoreHeat.CurrentSeizureChance:P0}";
            }

            SendDeclanMessage(reply);
        }

        public static void OnPlayerDeclinedDeal()
        {
            if (State != OffshoreState.AwaitingProduct)
            {
                string[] nothingToDecline = new[]
                {
                    "Declan: Nothing to decline - there's no deal on the table right now.",
                    "Declan: No active order to turn down. The books are clean.",
                    "Declan: We've got nothing lined up, so nothing to walk away from."
                };
                SendDeclanMessage(nothingToDecline[Rng.Next(nothingToDecline.Length)]);
                return;
            }

            string drug = CurrentOrder.DrugDisplayName;
            int quantity = CurrentOrder.RequiredQuantity;
            string destination = CurrentOrder.Destination;

            State = OffshoreState.Idle;
            _orderCooldownTimer = 0f;
            _negotiationAttempts = 0;
            _orderNegotiated = false;

            CaptainDeclanCross.ResumeSchedule();

            if (CaptainDeclanCross.Instance != null)
            {
                OffshoreDialogue.SyncActiveContainer(CaptainDeclanCross.Instance.Dialogue);
            }



            string[] declineReplies = new[]
            {
                $"Declan: Fair enough. I'll tell the buyers in {destination} the {quantity}x {drug} is off. No hard feelings - there'll be other orders.",
                $"Declan: Deal's off then. {destination} won't be happy, but they'll live. I'll line up the next one when it comes.",
                $"Declan: Understood - {quantity}x {drug} back off the manifest. I'll text you when another buyer bites.",
                $"Declan: Your call, friend. The {destination} run is dead. I'll keep my phone on for the next one."
            };
            SendDeclanMessage(declineReplies[Rng.Next(declineReplies.Length)]);
        }

        /// <summary>
        /// The player asking Declan, from the phone, to line a run up there and then.
        ///
        /// He takes it seven times out of ten. When he does not, he makes the player wait five real
        /// minutes before asking again. Only a turn-down costs that wait: a request he answers for a
        /// reason - the lane is too hot, the name is not big enough - is answered straight away and
        /// leaves the clock alone, so being told no for a real reason never burns the five minutes.
        ///
        /// A run he agrees to is the one and only one of these he does. The ask skips his own order
        /// clock, so being able to lean on it whenever the lane went quiet would make his regular
        /// texts pointless; see <see cref="OffshoreHeat.RunNowUsed"/>.
        /// </summary>
        public static void OnPlayerRequestedRunNow()
        {
            if (OffshoreHeat.RunNowUsed)
            {
                SendDeclanMessage(
                    "Declan: I've already pulled a run out of thin air for you once. That was a favour, not a service - wait on my texts like everyone else.");
                return;
            }

            if (State != OffshoreState.Idle)
            {
                SendDeclanMessage(
                    "Declan: Easy, sailor. We've already got a run on the books. See that one through before we line up another.");
                return;
            }

            if (_runNowRefusalTimer > 0f)
            {
                int minutesLeft = Mathf.Max(1, Mathf.CeilToInt(_runNowRefusalTimer / 60f));
                string waitWord = minutesLeft == 1 ? "minute" : "minutes";

                SendDeclanMessage(
                    $"Declan: Not this minute. I need time to line a manifest up first - give me {minutesLeft} more {waitWord} and ask me again.");
                return;
            }

            if (!OffshoreHeat.CanSail)
            {
                SendDeclanMessage(OffshoreHeat.GetRefusalMessage());
                return;
            }

            if (OffshoreHeat.IntroDealUsed && !OffshoreHeat.IsShotCaller)
            {
                SendDeclanMessage(
                    "Declan: Easy, sailor. I only run overseas cargo for names that carry weight now. Make Shot Caller and I'll text you the moment the lane opens up.");
                return;
            }

            // TriggerOffshoreSale sends the manifest itself when it takes, so nothing is said here.
            // The favour is only spent on a run he actually agreed to, so a turn-down never costs
            // the player their one shot at it.
            if (Rng.NextDouble() < RunNowAcceptChance)
            {
                if (TriggerOffshoreSale(true))
                {
                    OffshoreHeat.MarkRunNowUsed();
                }

                return;
            }

            _runNowRefusalTimer = RunNowRefusalCooldown;

            string[] refusals = new[]
            {
                "Declan: Can't right now, friend - the buyers aren't biting this minute. Give me five and ask me again.",
                "Declan: Not yet. I need a call back from the other side before I commit to anything. Five minutes, then try me.",
                "Declan: Hold your horses. There's nothing lined up on the far side yet. Come back to me in five."
            };

            SendDeclanMessage(refusals[Rng.Next(refusals.Length)]);
        }

        public static void SendDeclanMessage(string body)
        {
            ConfigurePhoneSendables();

            try
            {
                var declan = CaptainDeclanCross.Instance;
                if (declan != null)
                {
                    declan.SendTextMessage(body);
                    return;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                var declan = CaptainDeclanCross.Instance;
                if (declan != null && declan.gameObject != null)
                {
                    var npc = declan.gameObject.GetComponent<Il2CppScheduleOne.NPCs.NPC>()
                           ?? declan.gameObject.GetComponentInChildren<Il2CppScheduleOne.NPCs.NPC>(true);

                    if (npc != null)
                    {
                        if (npc.MSGConversation != null)
                        {
                            var msg = new Il2CppScheduleOne.Messaging.Message(body, Il2CppScheduleOne.Messaging.Message.ESenderType.Other);
                            npc.MSGConversation.SendMessage(msg, true, true);
                            return;
                        }
                        else
                        {
                            npc.SendTextMessage(body);
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            try
            {
                Il2CppScheduleOne.UI.HintDisplay.Instance?.ShowHint_20s("Declan: " + body);
            }
            catch { }
        }

        private static IEnumerable<Il2CppScheduleOne.ItemFramework.ItemSlot> EnumeratePlayerSlots(
            Il2CppScheduleOne.PlayerScripts.Player player)
        {
            var slots = new List<Il2CppScheduleOne.ItemFramework.ItemSlot>();

            try
            {
                var inv = UnityEngine.Object.FindObjectOfType<
                    Il2CppScheduleOne.PlayerScripts.PlayerInventory>();

                if (inv != null)
                {
                    var all = inv.GetAllInventorySlots();

                    if (all != null)
                    {
                        for (int i = 0; i < all.Count; i++)
                        {
                            if (all[i] != null)
                                slots.Add(all[i]);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            if (slots.Count == 0 && player != null && player._inventory != null)
            {
                for (int i = 0; i < player._inventory.Length; i++)
                {
                    if (player._inventory[i] != null)
                        slots.Add(player._inventory[i]);
                }
            }

            return slots;
        }

        public static int CountPlayerDrug(OffshoreOrderInfo order)
        {
            try
            {
                var player = Il2CppScheduleOne.PlayerScripts.Player.Local;
                if (player == null || player._inventory == null)
                    return 0;

                int total = 0;
                foreach (var slot in EnumeratePlayerSlots(player))
                {
                    if (slot == null || slot.ItemInstance == null || slot.ItemInstance.Definition == null)
                        continue;

                    string id = slot.ItemInstance.Definition.ID ?? "";
                    string defName = slot.ItemInstance.Definition.Name ?? "";
                    string instName = slot.ItemInstance.Name ?? "";

                    if (IsDrugMatch(id, defName, instName, order))
                    {
                        total += GetItemQuantityOrAmount(slot);
                    }
                }

                if (total <= 0)
                {
                    string dump = "[Offshore] Product not detected - order wants '" +
                                  order.DrugDisplayName + "'. Occupied slots:";

                    bool any = false;

                    int slotIndex = 0;

                    foreach (var s in EnumeratePlayerSlots(player))
                    {
                        if (s == null || s.ItemInstance == null ||
                            s.ItemInstance.Definition == null)
                        {
                            continue;
                        }

                        any = true;

                        dump += " | #" + slotIndex++ + " " + s.ItemInstance.Definition.ID +
                                " ('" + s.ItemInstance.Definition.Name + "' / '" +
                                s.ItemInstance.Name + "') qty=" + s.Quantity;
                    }

                    if (!any)
                        dump += " NONE (every slot empty)";


                }

                return total;
            }
            catch (Exception)
            {

                return 0;
            }
        }

        public static bool DeductPlayerDrug(OffshoreOrderInfo order, int amountNeeded, out int removedUnits, out bool tookExtraPackage, out int keptSampleUnits)
        {
            removedUnits = 0;
            tookExtraPackage = false;
            keptSampleUnits = 0;

            try
            {
                var player = Il2CppScheduleOne.PlayerScripts.Player.Local;
                if (player == null || player._inventory == null)
                    return false;

                var snapshot = new List<HandoverStack>();
                foreach (var slot in EnumeratePlayerSlots(player))
                {
                    if (slot == null || slot.ItemInstance == null || slot.ItemInstance.Definition == null)
                        continue;

                    string id = slot.ItemInstance.Definition.ID ?? "";
                    string defName = slot.ItemInstance.Definition.Name ?? "";
                    string instName = slot.ItemInstance.Name ?? "";

                    if (!IsDrugMatch(id, defName, instName, order))
                        continue;

                    var prod = slot.ItemInstance.TryCast<Il2CppScheduleOne.Product.ProductItemInstance>();
                    if (prod == null)
                        continue;

                    int slotUnits = GetItemQuantityOrAmount(slot);
                    if (slotUnits <= 0 || slot.Quantity <= 0)
                        continue;

                    if (snapshot.Count == 0)
                        LastHandoverQuality = prod.Quality;

                    int perPackage = prod.Amount > 0
                        ? prod.Amount
                        : Math.Max(1, slotUnits / slot.Quantity);

                    snapshot.Add(new HandoverStack { Slot = slot, PerPackage = perPackage });
                }

                string nativeDrugId = null;
                int availablePackages = 0;

                foreach (var stack in snapshot)
                {
                    if (nativeDrugId == null &&
                        stack.Slot != null &&
                        stack.Slot.ItemInstance != null &&
                        stack.Slot.ItemInstance.Definition != null)
                    {
                        nativeDrugId = stack.Slot.ItemInstance.Definition.ID;
                    }

                    if (stack.Slot != null)
                        availablePackages += stack.Slot.Quantity;
                }

                int packagesToTake = Math.Min(amountNeeded, availablePackages);
                int nativeRemoved = -1;

                if (nativeDrugId != null && packagesToTake > 0)
                {
                    int beforeCount = CountPlayerDrug(order);

                    try
                    {
                        var inv = UnityEngine.Object.FindObjectOfType<
                            Il2CppScheduleOne.PlayerScripts.PlayerInventory>();

                        if (inv != null)
                        {
                            inv.RemoveAmountOfItem(nativeDrugId, (uint)packagesToTake);
                        }
                        else
                        {

                        }
                    }
                    catch (Exception)
                    {

                    }

                    int afterCount = CountPlayerDrug(order);
                    nativeRemoved = Math.Max(0, beforeCount - afterCount);

                    if (nativeRemoved >= packagesToTake)
                    {
                        removedUnits = nativeRemoved;
                        tookExtraPackage = nativeRemoved > amountNeeded;
                        keptSampleUnits = 0;


                        return removedUnits > 0;
                    }


                }

                int remaining = Math.Max(0, packagesToTake - Math.Max(0, nativeRemoved));
                foreach (var stack in snapshot)
                {
                    if (remaining <= 0)
                        break;

                    int packages = Math.Min(stack.Slot.Quantity, remaining / stack.PerPackage);
                    if (packages > 0)
                    {
                        stack.Packages = packages;
                        remaining -= packages * stack.PerPackage;
                    }
                }

                if (remaining > 0)
                {
                    HandoverStack best = null;
                    foreach (var stack in snapshot)
                    {
                        int extra = stack.Slot.Quantity - stack.Packages;
                        if (extra <= 0)
                            continue;
                        if (stack.PerPackage >= remaining && (best == null || stack.PerPackage < best.PerPackage))
                            best = stack;
                    }

                    if (best == null)
                    {

                        foreach (var dbg in snapshot)
                        {

                        }

                        return false;
                    }

                    best.Packages++;
                    remaining -= best.PerPackage;
                    tookExtraPackage = true;
                }

                int packagesTaken = 0;
                int keptSample = 0;
                foreach (var stack in snapshot)
                {
                    if (stack.Packages <= 0)
                        continue;

                    var slot = stack.Slot;
                    int stockBefore = slot.Quantity;

                    if (stack.Packages >= stockBefore)
                    {
                        if (ClearSlotCompletely(slot))
                        {
                            removedUnits += stockBefore * stack.PerPackage;
                            packagesTaken += stockBefore;
                            WriteBackSlot(player, slot);
                            continue;
                        }

                        int takeable = Math.Max(0, stockBefore - 1);
                        int beforeQ = slot.Quantity;

                        if (takeable > 0)
                            ChangeQuantityNotifying(slot, -takeable);

                        int actualTaken = Math.Max(0, beforeQ - slot.Quantity);

                        removedUnits += actualTaken * stack.PerPackage;
                        packagesTaken += actualTaken;
                        keptSample += (stockBefore - actualTaken) * stack.PerPackage;



                        continue;
                    }

                    int partialBefore = slot.Quantity;

                    for (int p = 0; p < stack.Packages && slot.Quantity > 1; p++)
                    {
                        int before = slot.Quantity;
                        ChangeQuantityNotifying(slot, -1);
                        int actual = before - slot.Quantity;

                        if (actual <= 0)
                            break;

                        removedUnits += actual * stack.PerPackage;
                        packagesTaken += actual;
                    }

                    if (partialBefore != slot.Quantity)
                    {
                    }

                    WriteBackSlot(player, slot);
                }

                keptSampleUnits = keptSample;


                return remaining <= 0 && removedUnits > 0;
            }
            catch (Exception)
            {

                return false;
            }
        }

        private sealed class HandoverStack
        {
            public Il2CppScheduleOne.ItemFramework.ItemSlot Slot;
            public int PerPackage;
            public int Packages;
        }

        private static void WriteBackSlot(
            Il2CppScheduleOne.PlayerScripts.Player player,
            Il2CppScheduleOne.ItemFramework.ItemSlot slot)
        {
            try
            {
                int index = FindSlotIndex(player, slot);

                if (index >= 0)
                    player.SetInventoryItem(index, slot.ItemInstance);
            }
            catch (Exception)
            {

            }
        }

        private static int FindSlotIndex(
            Il2CppScheduleOne.PlayerScripts.Player player,
            Il2CppScheduleOne.ItemFramework.ItemSlot slot)
        {
            if (player == null || player._inventory == null || slot == null)
                return -1;

            for (int i = 0; i < player._inventory.Length; i++)
            {
                var s = player._inventory[i];

                if (s != null && s.Pointer == slot.Pointer)
                    return i;
            }

            return -1;
        }

        private static void ChangeQuantityNotifying(
            Il2CppScheduleOne.ItemFramework.ItemSlot slot,
            int delta)
        {
            if (slot == null || delta == 0)
                return;

            try
            {
                slot.ChangeQuantity(delta, false);
            }
            catch (Exception)
            {


                try
                {
                    slot.ChangeQuantity(delta, true);
                }
                catch (Exception)
                {

                }
            }
        }

        private static void DetachInstance(Il2CppScheduleOne.ItemFramework.ItemSlot slot)
        {
            if (slot == null || slot.ItemInstance == null)
                return;

            try
            {
                slot.ClearStoredInstance(false);
                if (slot.ItemInstance == null)
                    return;
            }
            catch (Exception)
            {

            }

            try
            {
                slot.ClearStoredInstance(true);
                if (slot.ItemInstance == null)
                    return;
            }
            catch (Exception)
            {

            }

            try
            {
                slot.SetStoredItem(null, false);
                if (slot.ItemInstance == null)
                    return;
            }
            catch (Exception)
            {

            }

            try
            {
                slot.SetStoredItem(null, true);
                if (slot.ItemInstance == null)
                    return;
            }
            catch (Exception)
            {

            }

            try
            {
                var property = HarmonyLib.AccessTools.Property(
                    slot.GetType(), "ItemInstance");

                property?.SetValue(slot, null);
            }
            catch (Exception)
            {

            }
        }

        private static bool ClearSlotCompletely(Il2CppScheduleOne.ItemFramework.ItemSlot slot)
        {
            if (slot == null)
                return false;

            try
            {
                if (slot.IsRemovalLocked || slot.IsAddLocked || slot.ActiveLock != null)
                {
                    slot.RemoveLock(true);
                }
            }
            catch (Exception)
            {

            }

            int stock = slot.Quantity;

            try
            {
                slot.ChangeQuantity(-stock, false);
                if (slot.Quantity <= 0 || slot.ItemInstance == null)
                {
                    DetachInstance(slot);
                    return true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                slot.SetQuantity(0, false);
                if (slot.Quantity <= 0 || slot.ItemInstance == null)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                slot.SetQuantity(0, false);
                if (slot.Quantity <= 0 || slot.ItemInstance == null)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                slot.ClearStoredInstance(false);
                if (slot.Quantity <= 0 || slot.ItemInstance == null)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                slot.ItemInstance = null;
                if (slot.Quantity <= 0 || slot.ItemInstance == null)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }

            try
            {
                slot.SetStoredItem(null, true);
                if (slot.Quantity <= 0 || slot.ItemInstance == null)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }



            return false;
        }

        public static string GetPlayerOtherHighProfitDrugs(OffshoreOrderInfo requestedOrder)
        {
            try
            {
                var player = Il2CppScheduleOne.PlayerScripts.Player.Local;
                if (player == null || player._inventory == null)
                    return null;

                List<string> found = new List<string>();
                string[] drugs = new[] { "MDMA", "Cocaine", "DMT", "Salvia", "THC Gummies" };

                for (int i = 0; i < player._inventory.Length; i++)
                {
                    var slot = player._inventory[i];
                    if (slot == null || slot.ItemInstance == null || slot.ItemInstance.Definition == null)
                        continue;

                    if (GetItemQuantityOrAmount(slot) <= 0)
                        continue;

                    string id = slot.ItemInstance.Definition.ID ?? "";
                    string defName = slot.ItemInstance.Definition.Name ?? "";
                    string instName = slot.ItemInstance.Name ?? "";

                    foreach (var drug in drugs)
                    {
                        if (string.Equals(drug, requestedOrder.DrugDisplayName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (MatchesDrugName(id, defName, instName, drug) && !found.Contains(drug))
                        {
                            found.Add(drug);
                        }
                    }
                }

                if (found.Count > 0)
                    return string.Join(" or ", found);
            }
            catch { }

            return null;
        }

        private static int GetItemQuantityOrAmount(Il2CppScheduleOne.ItemFramework.ItemSlot slot)
        {
            if (slot == null || slot.ItemInstance == null)
                return 0;

            try
            {
                var prod = slot.ItemInstance.TryCast<Il2CppScheduleOne.Product.ProductItemInstance>();
                if (prod != null)
                {
                    try
                    {
                        if (prod.AppliedPackaging == null)
                            return 0;
                    }
                    catch
                    {
                        return 0;
                    }

                    int perPackage = prod.Amount > 0 ? prod.Amount : 0;
                    if (perPackage > 0 && slot.Quantity > 0)
                        return perPackage * slot.Quantity;

                    int totalAmount = prod.GetTotalAmount();
                    if (totalAmount > 0)
                        return totalAmount;

                    if (prod.Amount > 0)
                        return prod.Amount * Math.Max(1, slot.Quantity);
                }
            }
            catch { }

            return slot.Quantity;
        }

        private static bool IsDrugMatch(string id, string defName, string instName, OffshoreOrderInfo order)
        {
            return MatchesDrugName(id, defName, instName, order.DrugDisplayName);
        }

        public static bool MatchesDrugName(string id, string defName, string instName, string drug)
        {
            if (string.Equals(drug, "MDMA", StringComparison.OrdinalIgnoreCase))
            {
                return Contains(id, "mdma") || Contains(defName, "mdma") || Contains(instName, "mdma");
            }

            if (string.Equals(drug, "Cocaine", StringComparison.OrdinalIgnoreCase))
            {
                return Contains(id, "cocaine") || Contains(id, "coke") ||
                       Contains(defName, "cocaine") || Contains(instName, "cocaine");
            }

            if (string.Equals(drug, "DMT", StringComparison.OrdinalIgnoreCase))
            {
                return Contains(id, "dmt") || Contains(defName, "dmt") || Contains(instName, "dmt");
            }

            if (string.Equals(drug, "Salvia", StringComparison.OrdinalIgnoreCase))
            {
                return Contains(id, "salvia") || Contains(defName, "salvia") || Contains(instName, "salvia");
            }

            if (string.Equals(drug, "THC Gummies", StringComparison.OrdinalIgnoreCase))
            {
                return Contains(id, "thc_gummies") || Contains(id, "thcgummies") || Contains(id, "gumm") ||
                       Contains(defName, "gumm") || Contains(instName, "gumm");
            }

            return false;
        }

        private static bool Contains(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return false;
            return source.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AwardCash(float amount)
        {
            try
            {
                var mm = UnityEngine.Object.FindObjectOfType<Il2CppScheduleOne.Money.MoneyManager>();
                if (mm != null)
                {
                    mm.ChangeCashBalance(amount, true, true);
                    mm.PlayCashSound();
                    return;
                }
            }
            catch (Exception)
            {

            }

            CustomNPCExample.Utils.WvcGiveItem.TryGive("cash", (int)amount);
        }
    }
}
