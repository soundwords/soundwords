using System.Linq;
using ServiceStack.Auth;
using ServiceStack.Data;

namespace SoundWords
{
    public class CustomOrmLiteAuthRepository : OrmLiteAuthRepository
    {
        public CustomOrmLiteAuthRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
        {
        }

        public CustomOrmLiteAuthRepository(IDbConnectionFactory dbFactory, string namedConnnection = null) : base(dbFactory, namedConnnection)
        {
        }

        public override IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            string[] nameParts = newUser.DisplayName.Split(' ');
            newUser.FirstName = string.Join(" ", nameParts.Take(nameParts.Length - 1));
            newUser.LastName = nameParts.Last();

            return base.CreateUserAuth(newUser, password);
        }
    }
}