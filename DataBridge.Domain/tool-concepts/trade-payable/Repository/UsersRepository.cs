using Dapper;
using Domain.Models;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using System.Data;


namespace Infrastructure.Repository
{
    public class UsersRepository(DapperContext context) : IUsersRepository
    {
        private readonly DapperContext _context = context;

        public async Task<UserInfoQueryResponse> GetAuthorizedUser(string UserPSNO)
        {
            var user_details = new UserInfoQueryResponse();
            var msg = new Message();
            var query = "SP_GetUserDetailsByPSNO";
            using var connection = _context.CreateConnection("default");

            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@PSNO", UserPSNO, DbType.String, direction: ParameterDirection.Input);
                var user = await connection.QueryAsync<UserInfo>(query, dynamicParameters, commandType: CommandType.StoredProcedure);

                if(user == null || !user.Any())
                {
                    msg.Success = false;
                    msg.Text = "User not found.";
                    user_details.Message = msg;
                    user_details.User = null;
                    return user_details;
                }

                var userInfo = user.FirstOrDefault();
                msg.Success = true;
                msg.Text = "User details fetched successfully.";
                user_details.Message = msg;
                user_details.User = userInfo;
                return user_details;
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = ex.Message;
                user_details.Message = msg;
                user_details.User = null;
            }

            return user_details;
        }

    }
}
