using Compost.Core.Models;
using System.Threading.Tasks;

namespace Compost.Core.Interfaces;

public interface IGitCredentialProvider
{
    Task<GitCredential> GetDefaultCredentialAsync();
}
