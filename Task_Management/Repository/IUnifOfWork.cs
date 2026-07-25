namespace Task_Management.Repository
{
    public interface IUnifOfWork 
    {
        ITaskRepository Tasks { get; }
        IProjectRepository Projects { get; }
        Task<int> CommitAsync();
    }
}
