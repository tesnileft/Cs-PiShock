namespace CsPiShock;

public class UserCredentialException : Exception
{
    public UserCredentialException(string e = "Invalid credentials.") : base(e)
    {
        
    }
}