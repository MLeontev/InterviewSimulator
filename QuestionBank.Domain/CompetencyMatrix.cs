namespace QuestionBank.Domain;

public class CompetencyMatrix
{
    public Guid Id { get; set; }
    
    public Guid GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    
    public Guid SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;
    
    public List<CompetencyMatrixItem> Competencies { get; set; } = [];
}