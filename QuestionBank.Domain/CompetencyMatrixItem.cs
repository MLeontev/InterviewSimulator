namespace QuestionBank.Domain;

public class CompetencyMatrixItem
{
    public int Id { get; set; }
    
    public int CompetencyId { get; set; }
    public Competency Competency { get; set; } = null!;
    
    public double Weight { get; set; }
}