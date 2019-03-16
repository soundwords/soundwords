// This software is part of the Autofac IoC container
// Copyright © 2011 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;

namespace SoundWords
{
    public static class AutofacExtensions
    {
        /// <summary>
        /// Specifies that a type from a scanned assembly is registered as providing all of its
        /// default interfaces.
        /// </summary>
        /// <typeparam name="TLimit">Registration limit type.</typeparam>
        /// <param name="registration">Registration to set service mapping on.</param>
        /// <returns>Registration builder allowing the registration to be configured.</returns>
        public static IRegistrationBuilder<TLimit, ScanningActivatorData, DynamicRegistrationStyle>
            AsDefaultInterface<TLimit>(this IRegistrationBuilder<TLimit, ScanningActivatorData, DynamicRegistrationStyle> registration)
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));

            return registration.As(t => GetDefaultInterfaces(t));
        }

        /// <summary>
        /// Specifies that a type is registered as providing all of its default interfaces.
        /// </summary>
        /// <typeparam name="TLimit">Registration limit type.</typeparam>
        /// <typeparam name="TConcreteActivatorData">Activator data type.</typeparam>
        /// <param name="registration">Registration to set service mapping on.</param>
        /// <returns>Registration builder allowing the registration to be configured.</returns>
        public static IRegistrationBuilder<TLimit, TConcreteActivatorData, SingleRegistrationStyle>
            AsDefaultInterface<TLimit, TConcreteActivatorData>(this IRegistrationBuilder<TLimit, TConcreteActivatorData, SingleRegistrationStyle> registration)
            where TConcreteActivatorData : IConcreteActivatorData
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));

            return registration.As(GetDefaultInterfaces(registration.ActivatorData.Activator.LimitType));
        }

        /// <summary>
        /// Specifies that a type is registered as providing all of its default interfaces.
        /// </summary>
        /// <typeparam name="TLimit">Registration limit type.</typeparam>
        /// <param name="registration">Registration to set service mapping on.</param>
        /// <returns>Registration builder allowing the registration to be configured.</returns>
        public static IRegistrationBuilder<TLimit, ReflectionActivatorData, DynamicRegistrationStyle>
            AsDefaultInterface<TLimit>(this IRegistrationBuilder<TLimit, ReflectionActivatorData, DynamicRegistrationStyle> registration)
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));

            var implementationType = registration.ActivatorData.ImplementationType;
            return registration.As(GetDefaultInterfaces(implementationType));
        }

        private static Type[] GetDefaultInterfaces(Type type)
        {
            var implementedInterfaces = type.GetTypeInfo().ImplementedInterfaces.ToArray();

            var inheritedInterfaces = implementedInterfaces.SelectMany(i => i.GetTypeInfo().ImplementedInterfaces).Distinct();

            var interfaces = implementedInterfaces.Except(inheritedInterfaces).Where(i => i.Name == $"I{type.Name}");

            return interfaces.ToArray();
        }
    }
}
