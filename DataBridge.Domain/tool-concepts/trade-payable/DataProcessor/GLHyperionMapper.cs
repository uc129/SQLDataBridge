using Domain.Aggregates.Static_Master_Tables;
using Infrastructure.Contracts;


namespace Application.Data_Cleaning
{
    public class GLHyperionMapper
    {

        private static Dictionary<string, GLAccountMapping> _glCodeHyperionMap = [];
        private static bool _isInitialized = false;

        public class GLAccountMapping(string hyperionCode, string hypDescription, string billedStatus)
        {
            public string HyperionCode { get; } = hyperionCode;
            public string HyperionCodeDescription { get; } = hypDescription;
            public string BilledStatus { get; } = billedStatus;
        }


        public static async Task InitializeAsync(IGLHyperionMapRepository hyprepo)
        {
            if (_isInitialized)
            {
                return;
            }

            // 1. Fetch data asynchronously
            IEnumerable<GLHyperionMap> hyperionTable = await hyprepo.GetAllAsync();

            // 2. Create the dictionary from the data
            var gl_hyp_map = new Dictionary<string, GLAccountMapping>();
            foreach (var row in hyperionTable)
            {
                gl_hyp_map.Add(
                    row.GLCode,
                    new GLAccountMapping(row.Hyperion_Code, row.Hyperion_Description, row.Billed_Status)
                );
            }

            // 3. ATOMICALLY assign the fully built map and set the flag
            _glCodeHyperionMap = gl_hyp_map;
            _isInitialized = true;
        }



        // ----------------------------------------------------
        // PUBLIC ACCESSOR METHODS
        // ----------------------------------------------------

        public static GLAccountMapping GetMapping(string glAccount)
        {
            // The default dictionary handles the case where it's not initialized, 
            // but an explicit check is safer for critical applications.
            if (!_isInitialized)
            {
                // Throwing an exception here forces the calling code to fix the initialization.
                throw new InvalidOperationException("GLHyperionMapper has not been initialized. Call InitializeAsync() before calling GetMapping.");
            }

            GLAccountMapping defaultReturnValue = new("Not Mapped", "NA", "NA");

            // Accesses the initialized static field.
            if (_glCodeHyperionMap.TryGetValue(glAccount, out GLAccountMapping? mapping))
                return mapping;
            else return defaultReturnValue;
        }

        public static GLAccountMapping ProcessGlAccount(string glAccount)
        {
            GLAccountMapping mapping = GetMapping(glAccount);
            return mapping;
        }
    }
}