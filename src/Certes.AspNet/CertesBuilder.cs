using Microsoft.Extensions.DependencyInjection;

namespace Certes.AspNet
{
    public class CertesBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> services are attached to.
        /// </summary>
        /// <value>
        /// The <see cref="IServiceCollection"/> services are attached to.
        /// </value>
        public IServiceCollection Services { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertesBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public CertesBuilder(IServiceCollection services)
        {
            this.Services = services;
        }
    }
}
