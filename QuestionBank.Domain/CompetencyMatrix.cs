namespace QuestionBank.Domain;

public class CompetencyMatrix
{
    public int Id { get; set; }
    
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    
    public int SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;
    
    public List<CompetencyMatrixItem> Competencies { get; set; } = [];
}