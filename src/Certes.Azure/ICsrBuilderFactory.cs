using Certes.Pkcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public interface ICsrBuilderFactory
    {
        ICertificationRequestBuilder Create();
    }
}
