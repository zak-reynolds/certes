using Certes.Acme;
using System.Threading.Tasks;

namespace Certes.AspNet
{
    public interface IChallengeResponder
    {
        string ChallengeType { get; }
        Task Deploy(Challenge challenge);
    }
}
