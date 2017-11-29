using System;

namespace HrBot.Dialogs
{
    [Serializable]
    public class PastQnA
    {
        public string ConversationId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}