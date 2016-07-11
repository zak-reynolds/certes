using Microsoft.Extensions.DependencyInjection;

namespace Certes.AspNet
{
    public class CertesOptionsBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> services are attached to.
        /// </summary>
        /// <value>
        /// The <see cref="IServiceCollection"/> services are attached to.
        /// </value>
        public IServiceCollection Services { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertesOptionsBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public CertesOptionsBuilder(IServiceCollection services)
        {
            this.Services = services;
        }
    }
}
