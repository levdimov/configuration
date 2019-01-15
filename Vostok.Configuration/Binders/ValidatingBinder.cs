using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Abstractions.Attributes;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.Configuration.Binders
{
    internal class ValidatingBinder : ISettingsBinder
    {
        private readonly ISettingsBinder binder;

        public ValidatingBinder(ISettingsBinder binder)
        {
            this.binder = binder;
        }

        public TSettings Bind<TSettings>(ISettingsNode rawSettings)
        {
            var value = binder.Bind<TSettings>(rawSettings);
            Validate(value);
            return value;
        }

        private static void Validate<TSettings>(TSettings value)
        {
            var type = typeof(TSettings);

            var errors = Validate(type, value, "").ToList();
            if (errors.Any())
            {
                throw new SettingsValidationException(string.Join(Environment.NewLine, errors));
            }
        }

        private static IEnumerable<string> Validate(Type type, object value, string prefix)
        {
            if (value == null)
                yield break;
            if (!(type.GetCustomAttributes(typeof(ValidateByAttribute), false).FirstOrDefault() is ValidateByAttribute validateByAttribute))
                yield break;

            var validator = Activator.CreateInstance(validateByAttribute.ValidatorType);
            var validateMethod = validator.GetType().GetMethod(nameof(ISettingsValidator<object>.Validate), new[] {type});
            if (validateMethod == null)
                throw new SettingsValidationException($"Type '{validator.GetType()}' specified as validator for settings of type '{type}' does not contain a suitable {nameof(ISettingsValidator<object>.Validate)} method.");
            foreach (var error in (IEnumerable<string>)validateMethod.Invoke(validator, new[] {value}))
                yield return FormatError(prefix, error);

            foreach (var field in type.GetFields())
            {
                foreach (var error in Validate(field.FieldType, field.GetValue(value), field.Name))
                    yield return FormatError(prefix, error);
            }

            foreach (var prop in type.GetProperties())
            {
                foreach (var error in Validate(prop.PropertyType, prop.GetValue(value), prop.Name))
                    yield return FormatError(prefix, error);
            }
        }

        private static string FormatError(string prefix, string error) => prefix == "" ? error : prefix + "." + error;
    }
}