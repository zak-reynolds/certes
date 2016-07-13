using Certes.Acme;
using System.Threading.Tasks;

namespace Certes.Integration
{
    /// <summary>
    /// Supports responding to ACEM challenge.
    /// </summary>
    public interface IChallengeResponder
    {
        /// <summary>
        /// Gets the type of the challenge.
        /// </summary>
        /// <value>
        /// The type of the challenge.
        /// </value>
        string ChallengeType { get; }

        /// <summary>
        /// Deploys the resources to meet the specified challenge.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <returns>The awaitable.</returns>
        Task Deploy(Challenge challenge);

        /// <summary>
        /// Removes the resources deployed for the challenge.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <returns></returns>
        Task Remove(Challenge challenge);
    }
}
