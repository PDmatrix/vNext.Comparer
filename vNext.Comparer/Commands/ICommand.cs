using System.Threading.Tasks;

namespace vNext.Comparer.Commands
{
    public interface ICommand
    {
        Task Execute();
    }
}