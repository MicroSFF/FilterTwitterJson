using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Application to filter Twitter achive into a slimmer JSON with crud filtered out
/// Licenced under MIT license (see EOF comment)
/// Written by O. Westin http://microsff.com https://twitter.com/MicroSFF
/// </summary>
namespace FilterTwitterJson
{
    /// <summary>
    /// Utility class to validate and organise command-line arguments and 
    /// parameters.
    /// Expects the format -argument1 parameter-for-argument1 -argument2
    /// </summary>
    class ArgParser
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="toLower">If true, all arguments converted to lowercase</param>
        /// <param name="argMarker">Character indicating argument name follows</param>
        public ArgParser(bool toLower, char argMarker = '-')
        {
            this.toLower = toLower;
            this.argMarker = argMarker;
        }

        /// <summary>
        /// Add argument definition.
        /// Use multiple names to cater to short/verbose names for the same argument, e.g. -v or --verbose
        /// </summary>
        /// <param name="names">Argument names (must start with argMarker)</param>
        /// <param name="mandatory">True if argument is mandatory</param>
        /// <param name="minParams">Minimum number of parameters</param>
        /// <param name="maxParams">Maximum number of parameters (-1 to have no limit)</param>
        /// <param name="description">Description of argument</param>
        /// <param name="altMandatory">If true, as long as this argument is present, other mandatory arguments can be omitted. Intended to be used for --help etc.</param>
        public void AddDefinition(string[] names, bool mandatory, int minParams, int maxParams, string description, bool altMandatory = false)
        {
            // Make sure names are unique and start with argument marker
            foreach (var name in names)
            {
                if (name.IndexOf(argMarker) != 0)
                    throw new Exception(string.Format("Missing argument marker: {0}", string.Join(' ', names)));
                
                foreach (var param in definitions)
                {
                    if (param.names.Contains(name) || (toLower && param.names.Contains(name.ToLower())))
                        throw new Exception(string.Format("Duplicate argument definition: {0} and {1}", string.Join(' ', param.names), string.Join(' ', names)));
                }
            }
            definitions.Add(new ArgumentDefinition(names, mandatory, minParams, maxParams, description, altMandatory, toLower, argMarker));
        }

        /// <summary>
        /// Validate arguments against definitions
        /// Returns Dictionary with the first name in the definition as key, and a list of parameters (can be null)
        /// </summary>
        /// <param name="args">Application arguments</param>
        /// <returns>[first name of argument]->[list of parameters]</returns>
        public Dictionary<string, List<string>> ValidateArgs(string[] args)
        {
            // First, sort arguments and parameters
            Dictionary<string, List<string>> parameters = MakeArgumentsAndParameters(args);
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            // Second, check we have all mandatory arguments (or one alt)
            string mandatoryException = null;
            foreach (var def in definitions)
            {
                if (!def.mandatory && !def.altMandatory)
                    continue;
                bool found = false;
                foreach (var name in def.names)
                {
                    if (parameters.ContainsKey(name))
                    {
                        found = true;
                        break;
                    }
                }
                // Throw if any mandatory is missing, unless an alt mandatory is present
                if (!found && !def.altMandatory)
                {
                    mandatoryException = string.Format("Missing required argument \"{0}\": {1}", string.Join(' ', def.names), def.description);
                }
                else if (found && def.altMandatory)
                {
                    mandatoryException = null;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(mandatoryException))
                throw new Exception(mandatoryException);

            // Validate arguments
            foreach (var key in parameters.Keys)
            {
                bool found = false;
                foreach (var def in definitions)
                {
                    if (def.names.Contains(key))
                    {
                        // Check it's unique
                        int instances = 0;
                        foreach (var name in def.names)
                        {
                            if (parameters.ContainsKey(name))
                                ++instances;
                        }
                        if (instances != 1)
                            throw new Exception(string.Format("Duplicated argument: {0}", string.Join(' ', def.names)));

                        // Count arguments
                        List<string> arguments = parameters[key];
                        if ((def.minArgs > 0) && ((arguments == null) || (arguments.Count < def.minArgs)))
                            throw new Exception(string.Format("Too few ({0} required) parameters for \"{1}\": {2}", def.minArgs, string.Join(' ', def.names), def.description));
                        if ((def.maxArgs >= 0) && (arguments != null) && (arguments.Count > def.maxArgs))
                            throw new Exception(string.Format("Too many (max {0}) parameters for \"{1}\": {2}", def.maxArgs, string.Join(' ', def.names), def.description));

                        found = true;

                        // Normalise on first name
                        result[def.names[0]] = arguments;
                    }
                    if (found)
                        break;
                }
                if (!found)
                    throw new Exception(string.Format("Unknown argument: \"{0}\"", key));
            }
            return result;
        }

        /// <summary>
        /// List all definitions:
        /// names \t parameters \t description
        /// </summary>
        /// <returns>Summary of definitions, one per line</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var def in definitions)
            { 
                string parameters = "<no parameters>";
                if (def.maxArgs != 0)
                {
                    if (def.maxArgs > 0)
                    {
                        if (def.minArgs == def.maxArgs)
                            parameters = string.Format("<{0} parameter(s)>", def.minArgs);
                        else
                            parameters = string.Format("<{0} - {1} parameter(s)>", def.minArgs, def.maxArgs);
                    }
                    else
                        parameters = string.Format("<{0} - n parameters>", def.minArgs);
                }
                if (def.mandatory)
                    sb.AppendLine(string.Format("{0}\t{1}\t{2}", string.Join(", ", def.names), parameters, def.description));
                else
                    sb.AppendLine(string.Format("[{0}]\t{1}\t{2}", string.Join(", ", def.names), parameters, def.description));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Split list of strings into arguments (strings starting with argMarker) and their parameters
        /// </summary>
        /// <param name="args">List of strings</param>
        /// <returns>Dictionary with argument names as keys and parameters as values</returns>
        private Dictionary<string, List<string>> MakeArgumentsAndParameters(string[] args)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            string key = null;
            List<string> value = null;
            foreach (var arg in args)
            {
                if (arg.IndexOf(argMarker) == 0)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        result.Add(key, value);
                    }
                    if (toLower)
                        key = arg.ToLower();
                    else
                        key = arg;
                    value = null;
                }
                else if (!string.IsNullOrEmpty(key))
                {
                    if (value == null)
                    {
                        value = new List<string>();
                    }
                    value.Add(arg);
                }
                else
                {
                    // We don't have a key (e.g. argument) yet
                    throw new Exception(string.Format("Not an argument name: {0}", arg));
                }
            }
            if (!string.IsNullOrEmpty(key))
            {
                result.Add(key, value);
            }
            return result;
        }

        /// <summary>
        /// An argument definition, detailing how to parse and validate an argument and its parameters
        /// </summary>
        private struct ArgumentDefinition
        {
            /// <summary>
            /// Initializes and argument definition
            /// </summary>
            /// <param name="names">Argument names</param>
            /// <param name="mandatory">True if argument is mandatory</param>
            /// <param name="minArgs">Minimum number of parameters</param>
            /// <param name="maxArgs">Maximum number of parameters (-1 for unlimited)</param>
            /// <param name="description">Argument description</param>
            /// <param name="altMandatory">If true, if this argument is present, no mandatory arguments need be mandatory</param>
            /// <param name="toLower">If true, makes argument names lower case</param>
            /// <param name="argMarker">Character indicating start of argument name</param>
            public ArgumentDefinition(string[] names, bool mandatory, int minArgs, int maxArgs, string description, bool altMandatory, bool toLower, char argMarker)
            {
                if (names.Length < 1)
                    throw new Exception("No names given");
                if (toLower)
                {
                    this.names = new List<string>();
                    foreach (var name in names)
                        this.names.Add(name.ToLower());
                }
                else
                {
                    this.names = new List<string>(names);
                }
                foreach (var name in this.names)
                {
                    if (name.IndexOf(argMarker) != 0)
                        throw new Exception(string.Format("Missing '{0}' in parameter name: {1}", argMarker, this.names));
                }
                if (mandatory && altMandatory)
                    throw new Exception(string.Format("Argument can not be both mandatory and alt mandatory: {0}", this.names));
                this.mandatory = mandatory;
                this.altMandatory = altMandatory;

                if ((maxArgs < minArgs) && (maxArgs >=0))
                    throw new Exception(string.Format("Min args > max args in parameter: {0}", this.names));
                this.minArgs = minArgs;
                this.maxArgs = maxArgs;
                this.description = description;
            }
            public List<string> names;
            public bool mandatory;
            public int minArgs;
            public int maxArgs;
            public string description;
            public bool altMandatory;
        }

        bool toLower = false;
        char argMarker = '-';
        List<ArgumentDefinition> definitions = new List<ArgumentDefinition>();
    }
}
/*
Copyright 2020 O. Westin 

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in 
the Software without restriction, including without limitation the rights to 
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

Except as contained in this notice, the name(s) of the above copyright holders
shall not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
THE SOFTWARE.
*/
