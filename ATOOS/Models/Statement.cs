namespace ATOOS.Models
{
    public class Statement
    {
        public string Content { get; set; }
        public int Position { get; set; }
        public StatementType Type { get; set; }
    }

    public enum StatementType
    {
        Control = 0,
        Assignment = 1,
        Uncategorized = 2,
        Return = 3
    }
}