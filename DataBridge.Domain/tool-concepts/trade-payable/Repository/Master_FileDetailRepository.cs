using Dapper;
using Domain.Models;
using Domain.Shared;
using Infrastructure.Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public interface IMaster_FileDetail
    {
        Task<Message> InsertFileDetails(FilesEntity request);
        Task<FilesEntity> GetFileDetails(int id_file);
    }
    public class Master_FileDetailRepository : IMaster_FileDetail
    {
        private readonly DapperContext _context;
        public Master_FileDetailRepository(DapperContext context) { this._context = context; }
        public async Task<Message> InsertFileDetails(FilesEntity request)
        {
            var query = "sp_InsertFileDetails";
            Message _msg = new Message();
            try
            {

                using (var connection = _context.CreateConnection("default"))
                {
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@FileName", request.FileName, DbType.String, direction: ParameterDirection.Input);
                    dynamicParameters.Add("@FileDesc", request.FileDesc, DbType.String, direction: ParameterDirection.Input);
                    dynamicParameters.Add("@FilePath", request.FilePath, DbType.String, direction: ParameterDirection.Input);
                    dynamicParameters.Add("@FileExt", request.FileExt, DbType.String, direction: ParameterDirection.Input);
                    dynamicParameters.Add("@FileSize", request.FileSize, DbType.Int32, direction: ParameterDirection.Input);
                    dynamicParameters.Add("@ContainerName", request.ContainerName, DbType.String, direction: ParameterDirection.Input);
                    dynamicParameters.Add("@UUID", request.Actionby, DbType.String, direction: ParameterDirection.Input);

                    var result = await connection.QueryFirstAsync<string>(query, param: dynamicParameters, commandType: CommandType.StoredProcedure);
                    string res = Convert.ToString(result);
                    if (res.StartsWith("success:"))
                    {
                        _msg.Text = res.Split(':').Last();
                        _msg.Success = true;
                        //await UpdateEncUrl(_msg.Text);
                    }
                    else
                    {
                        _msg.Text = res.Split(':').Last();
                        _msg.Success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _msg.Success = false;
                _msg.Text = ex.Message;
            }
            return _msg;

        }

        //public async Task<Message> UpdateEncUrl(string id_file)
        //{
        //    var query = "sp_UpdateFileDetails";
        //    Message _msg = new Message();
        //    try
        //    {

        //        using (var connection = _context.CreateConnection())
        //        {
        //            var dynamicParameters = new DynamicParameters();
        //            dynamicParameters.Add("@id_file", int.Parse(id_file), DbType.String, direction: ParameterDirection.Input);
        //            dynamicParameters.Add("@OutUrl", "", DbType.String, direction: ParameterDirection.Input);

        //            var result = await connection.ExecuteAsync(query, param: dynamicParameters, commandType: CommandType.StoredProcedure);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _msg.IsSuccess = false;
        //        _msg.Text = ex.Message;
        //    }
        //    return _msg;

        //}
        public async Task<FilesEntity> GetFileDetails(int id_file)
        {
            var query = $"SELECT * FROM [dbo].[Master_FileDetail] WHERE [id_file] = {id_file};";

            using (var connection = _context.CreateConnection("default"))
            {
                return await connection.QueryFirstOrDefaultAsync<FilesEntity>(query);
            }

        }
    }
}
