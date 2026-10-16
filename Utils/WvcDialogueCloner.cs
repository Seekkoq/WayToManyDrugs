namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Retired. This used to clone a vanilla NPC's <c>DialogueContainer</c> node-for-node and
    /// set the copy as that NPC's dialogue override. Game build 0.4.7 replaced
    /// <c>DialogueContainer</c> with a new conversation model, so the node-graph surgery here
    /// referenced a type that no longer exists and threw at runtime.
    ///
    /// The casino and pawnshop laundering dialogues now build and arm their own container
    /// through S1API's <c>NPCDialogue.BuildAndRegisterContainer</c> +
    /// <c>UseContainerOnInteract</c>, so this cloner is no longer used. The class is kept only
    /// so existing documentation references still resolve.
    /// </summary>
    public static class WvcDialogueCloner
    {
    }
}
