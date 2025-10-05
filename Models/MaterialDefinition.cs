namespace EliteDataRelay.Models
{
    public class MaterialDefinition
    {
        public string Name { get; }
        public string FriendlyName { get; }
        public string Category { get; }
        public int Grade { get; }
        public MaterialDefinition(string name, string friendlyName, string category, int grade)
        {
            Name = name;
            FriendlyName = friendlyName;
            Category = category;
            Grade = grade;
        }
    }
}