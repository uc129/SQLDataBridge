using Domain.Shared;
using System.Data;


namespace Application.ProcessSteps.ProcessStepsRepo
{
    public interface IStepResultsRepository
    {
        // C.R.E.A.T.E
        Task<Message> SaveAndReplaceStepResultAsync(
            DataTable data,
            Guid processId,
            int stepIndex 
        );

        Task<Message> SaveAndAppendStepResultAsync(
            DataTable data,
            Guid processId,
            int stepIndex
        );


        // R.E.T.R.I.E.V.E
        Task<DataTable> RetrieveStepResultAsync(
            Guid processId,
            int stepIndex
        );



        // R.E.T.R.I.E.V.E (Generic IEnumerable<T>)
        Task<IEnumerable<T>> RetrieveStepResultAsIEnumerableAsync<T>(
            Guid processId,
            int stepIndex
        ) where T : new(); // Constraint ensures T has a parameterless constructor


        // R.E.T.R.I.E.V.E (Paginated Generic IEnumerable<T>)
        Task<IEnumerable<T>> RetrievePaginatedStepResultAsIEnumerableAsync<T>(Guid processId, int stepIndex, int pageSize, int skip) where T : new();



        
    }



}
