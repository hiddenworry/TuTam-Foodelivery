using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.ModelsEnum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccess.Repositories.Implements
{
    public class UserRepository : IUserRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(FoodDonationDeliveryDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> FindUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserPermissions)
                .Include(u => u.Role)
                .Include(u => u.CollaboratorApplication)
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();
            return user;
        }

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserPermissions)
                .Include(u => u.Role)
                .Where(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> UpdateRefreshTokenAsync(
            Guid userId,
            string newRefreshToken,
            DateTime date
        )
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    // Update the user's refresh token in the database
                    user.RefreshToken = newRefreshToken;
                    user.RefreshTokenExpiration = date;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    return user;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public async Task<User?> CreateUserAsync(User user)
        {
            try
            {
                var rs = _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<User?> UpdateUserAsync(User user)
        {
            try
            {
                var rs = _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<User?> DeleteRefreshTokenAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = "";
                user.RefreshTokenExpiration = new DateTime();
                ; // set expridate before today
                var rs = _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            return null;
        }

        public async Task<User?> DeleteAccessTokenAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.AccessToken = "";
                var rs = _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            return null;
        }

        public async Task<User?> UpdateAccessTokenAsync(Guid userId, string newAccessToken)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    // Update the user's refresh token in the database

                    user.AccessToken = newAccessToken;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return null;
            }
            return null;
        }

        public async Task<User?> FindUserByEmailOrPhoneAsync(string str)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Email == str || u.Phone == str)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> FindUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RefreshToken == refreshToken)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> FindUserByVerifyCodeAsync(string verifyCode)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(
                    u =>
                        u.VerifyCode == verifyCode
                        && u.VerifyCodeExpiration >= SettedUpDateTime.GetCurrentVietNamTime()
                )
                .FirstOrDefaultAsync();
        }

        public async Task<User?> FindUserByOtpcodeAndPhoneAsync(string otp, string phone)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(
                    u =>
                        u.OtpCode == otp
                        && u.Phone == phone
                        && u.OtpCodeExpiration >= SettedUpDateTime.GetCurrentVietNamTime()
                )
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>?> FindUserAsyncExceptRole(
            string searchStr,
            List<Role> exceptRoles
        )
        {
            try
            {
                var query = _context.Users.AsQueryable();
                if (!string.IsNullOrEmpty(searchStr))
                {
                    searchStr = searchStr.Trim().ToLower();

                    query = query.Where(
                        u =>
                            (
                                (u.Email != null && u.Email.Trim().ToLower().Contains(searchStr))
                                || (
                                    u.Name != null
                                    && u.Name.Trim().ToLower().Contains(searchStr) == true
                                )
                                || (
                                    u.Phone != null
                                    && u.Phone.Trim().ToLower().Contains(searchStr) == true
                                )
                            )
                    );
                }
                if (exceptRoles.Count > 0)
                {
                    query = query.Where(
                        u => !exceptRoles.Contains(u.Role) && u.Status == UserStatus.ACTIVE
                    );
                }
                var items = await query.ToListAsync();
                return items;
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> FindUserByPhoneAsync(string str)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Phone == str)
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>?> FindUserExceptRole(string searchStr, List<Role> exceptRoles)
        {
            try
            {
                var query = _context.Users.AsQueryable();
                if (!string.IsNullOrEmpty(searchStr))
                {
                    searchStr = searchStr.Trim().ToLower();

                    query = query.Where(
                        u =>
                            (
                                (u.Email != null && u.Email.Trim().ToLower().Contains(searchStr))
                                || (
                                    u.Name != null
                                    && u.Name.Trim().ToLower().Contains(searchStr) == true
                                )
                                || (
                                    u.Phone != null
                                    && u.Phone.Trim().ToLower().Contains(searchStr) == true
                                )
                            )
                    );
                }
                if (exceptRoles.Count > 0)
                {
                    query = query.Where(
                        u => !exceptRoles.Contains(u.Role) && u.Status == UserStatus.ACTIVE
                    );
                }
                var items = await query.ToListAsync();
                return items;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<User>?> FindUserAsync(UserFilterRequest userFilterRequest)
        {
            try
            {
                var query = _context.Users.Include(u => u.Role).AsQueryable();
                if (!string.IsNullOrEmpty(userFilterRequest.KeyWord))
                {
                    var keyWord = userFilterRequest.KeyWord.Trim().ToLower();

                    query = query.Where(
                        u =>
                            (
                                (u.Email != null && u.Email.Trim().ToLower().Contains(keyWord))
                                || (
                                    u.Name != null
                                    && u.Name.Trim().ToLower().Contains(keyWord) == true
                                )
                                || (
                                    u.Phone != null
                                    && u.Phone.Trim().ToLower().Contains(keyWord) == true
                                )
                            )
                    );
                }
                if (userFilterRequest.UserStatus != null)
                {
                    query = query.Where(u => u.Status == userFilterRequest.UserStatus);
                }
                if (userFilterRequest.RoleIds != null && userFilterRequest.RoleIds.Count > 0)
                {
                    query = query.Where(
                        u => userFilterRequest.RoleIds.Any(roleId => u.RoleId == roleId)
                    );
                }

                var items = await query.ToListAsync();
                return items;
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> FindUserProfileByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();
            return user;
        }

        public async Task<User?> FindUserByIdInclueBranchAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserPermissions)
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();
            return user;
        }

        public async Task<List<User>?> FindBranchAdminForAssignToBranchAsync()
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Role)
                    .AsQueryable();
                query = query.Where(
                    u =>
                        u.Status == UserStatus.ACTIVE
                        && u.Role.Name == RoleEnum.BRANCH_ADMIN.ToString()
                        && u.Branch == null
                );
                var items = await query.ToListAsync();
                return items;
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> DeleteUserAsync(User user)
        {
            try
            {
                var rs = _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<User?> FindUserByRoleAsync(string role)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name == role)
                .FirstOrDefaultAsync();
            return user;
        }
    }
}
