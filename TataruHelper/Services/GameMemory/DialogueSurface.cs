namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// What the game is drawing the line in, which decides what a copy of it
    /// has to look like - and whether there can be one at all.
    ///
    /// This was a single "is it a subtitle" flag while there were two answers.
    /// There are more than two, and the one that matters most is the one a flag
    /// could not say: a bubble over a character's head, where the copy must not
    /// go up, because what the client says about where a bubble is cannot be
    /// used.
    /// </summary>
    public enum DialogueSurface
    {
        /// <summary>Nothing is being drawn that a copy could be placed over.</summary>
        None = 0,

        /// <summary>
        /// The dialogue box: a window with a wooden frame, the speaker's name
        /// on a dark strip in its top-left corner, and the line inside it.
        /// </summary>
        Window,

        /// <summary>
        /// A cutscene subtitle - Hydaelyn's lines - which the game draws as
        /// bare pale text over the picture, centred in a strip the width of the
        /// screen, with no window around it. A copy has to bring its own dark
        /// ground, or the line underneath reads straight through it.
        /// </summary>
        Subtitle,

        /// <summary>
        /// A notice the game puts in the dialogue window's place: "The New
        /// Adventurer status is applied to all players who have recently begun
        /// their adventure", and the like.
        ///
        /// Same window, same place, no wooden frame and nobody speaking - the
        /// game lays these on a dark ground instead. Told apart by the frame
        /// itself: the window draws its nine-grid when there is one and does
        /// not when there is not, which is the one thing about the two designs
        /// that a reader outside the game can see.
        /// </summary>
        Notice,

        /// <summary>
        /// The strip a cutscene puts a question and its answers in.
        ///
        /// Alone among everything here, the player has to click it. A copy over
        /// it has to keep saying which answer the cursor is on, or they choose
        /// blind - which is worse than reading the question in English.
        /// </summary>
        Choice,

        /// <summary>
        /// A speech bubble over a character's head.
        ///
        /// No copy is placed over one. The bubbles all live in a single addon
        /// which reports its own corner at the top-left of the screen and a
        /// size of sixty by forty-five, whatever is being said and wherever the
        /// character is standing - so there is no rectangle to cover. The line
        /// is translated and shown in the chat window like any other.
        /// </summary>
        Bubble
    }
}
