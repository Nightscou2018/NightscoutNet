namespace API.Services
{
    public interface IAuthorization
    {
        bool CheckHash(string givenHash);
    }
}