namespace EliteDataRelay.Models
{
    public class MaterialDefinition
    {
        public string Name { get; }
        public int Grade { get; }
        public MaterialDefinition(string name, int grade)
        {
            Name = name;
            Grade = grade;
        }
    }
}