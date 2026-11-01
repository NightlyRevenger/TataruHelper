namespace Translation.Models
{
    /// <summary>
    /// What sort of game text a line is, so the right prompt can be chosen.
    ///
    /// <see cref="Chat"/> is what players type to each other - quick, casual,
    /// full of jargon. <see cref="Story"/> is what the game itself says: NPC
    /// dialogue, cutscene subtitles and narration, which read better localized
    /// than translated line by line.
    /// </summary>
    public enum TranslationTextKind
    {
        Chat = 0,
        Story = 1
    }
}
