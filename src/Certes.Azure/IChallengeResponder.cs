using Certes.Acme;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public interface IChallengeResponder
    {
        string ChallengeType { get; }
        Task Deploy(Challenge challenge);
    }
}
