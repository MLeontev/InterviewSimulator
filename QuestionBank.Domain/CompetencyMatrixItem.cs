namespace QuestionBank.Domain;

public class CompetencyMatrixItem
{
    public Guid Id { get; set; }
    
    public Guid CompetencyId { get; set; }
    public Competency Competency { get; set; } = null!;
    
    public double Weight { get; set; }
}