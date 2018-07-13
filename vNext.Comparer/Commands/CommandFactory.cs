using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace vNext.Comparer.Commands
{
    public static class CommandFactory
    {
        public static ICommand Create(IDictionary<string, string> args)
        {
            var commandType = Assembly.GetExecutingAssembly().GetTypes().First(r => r.Name == args["COMMAND"]);
            return (ICommand) Activator.CreateInstance(commandType, args: args);
        }
    }
}