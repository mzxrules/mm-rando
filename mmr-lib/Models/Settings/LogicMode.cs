using System.ComponentModel;

namespace MMRando.Models
{
    public enum LogicMode
    {
        [Description("Casual")]
        Casual,
        [Description("Glitched Logic")]
        Glitched,
        [Description("Vanilla Logic")]
        Vanilla,
        [Description("User Logic")]
        UserLogic,
        [Description("No Logic")]
        NoLogic,
    }
}
