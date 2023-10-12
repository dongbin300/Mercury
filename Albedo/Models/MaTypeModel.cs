using Albedo.Enums;

namespace Albedo.Models
{
    public class MaTypeModel
    {
        public MaType Type { get; set; }
        public string Text { get; set; }

        public MaTypeModel(MaType type, string text)
        {
            Type = type;
            Text = text;
        }
    }
}
