using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SteamIrcBot
{
    class ConfigCommand : Command
    {
        public ConfigCommand()
        {
            Triggers.Add( "!config" );
            HelpText = "!config <get/set/add/remove/clear> <option> [value] - Modify config settings";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( !Settings.Current.IsAdmin( details.Sender ) )
                return;

            if ( details.Args.Length < 2 )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, HelpText );
                return;
            }

            string mode = details.Args[ 0 ];
            string option = details.Args[ 1 ];
            string value = string.Join( " ", details.Args.Skip( 2 ) );
                
            string[] scalarModes = { "get", "set" };
            string[] listModes = { "add", "remove", "Clear" };

            if ( scalarModes.Contains( mode, StringComparer.OrdinalIgnoreCase ) )
            {
                // scalar operation

                if ( mode.Equals( "get", StringComparison.OrdinalIgnoreCase ) )
                {
                    try
                    {
                        string configValue = GetConfigValue( option );
                        IRC.Instance.Send( details.Channel, "{0}: {1}: {2}", details.Sender.Nickname, option, configValue );
                    }
                    catch ( InvalidOperationException ex )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Unable to get value: {1}", details.Sender.Nickname, ex.Message );
                    }
                }
                else // set
                {
                    if ( details.Args.Length < 3 )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Missing required value for config field", details.Sender.Nickname );
                        return;
                    }

                    try
                    {
                        SetScalarConfigValue( option, value  );
                        IRC.Instance.Send( details.Channel, "{0}: Set!", details.Sender.Nickname );
                    }
                    catch ( InvalidOperationException ex )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Unable to set value: {1}", details.Sender.Nickname, ex.Message );
                    }
                }
            }
            else if ( listModes.Contains( mode, StringComparer.OrdinalIgnoreCase ) )
            {
                // list operation

                if ( mode.Equals( "add", StringComparison.OrdinalIgnoreCase ) )
                {
                    if ( details.Args.Length < 3 )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Missing required value for config field", details.Sender.Nickname );
                        return;
                    }

                    try
                    {
                        AddListConfigValue( option, value );
                        IRC.Instance.Send( details.Channel, "{0}: Added!", details.Sender.Nickname );
                    }
                    catch ( InvalidOperationException ex )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Unable to add list value: {1}", details.Sender.Nickname, ex.Message );
                    }
                }
                else if ( mode.Equals( "remove", StringComparison.OrdinalIgnoreCase ) )
                {
                    if ( details.Args.Length < 3 )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Missing required value for config field", details.Sender.Nickname );
                        return;
                    }

                    try
                    {
                        RemoveListConfigValue( option, value );
                        IRC.Instance.Send( details.Channel, "{0}: Removed!", details.Sender.Nickname );
                    }
                    catch ( InvalidOperationException ex )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Unable to remove list value: {1}", details.Sender.Nickname, ex.Message );
                    }
                }
                else // clear
                {
                    try
                    {
                        ClearListConfigValue( option );
                        IRC.Instance.Send( details.Channel, "{0}: Cleared!", details.Sender.Nickname );
                    }
                    catch ( InvalidOperationException ex )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Unable to clear config: {1}", details.Sender.Nickname, ex.Message );
                    }
                }
            }
            else
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid mode, should be one of: set, set, add, remove, clear", details.Sender.Nickname );
                return;
            }

            if ( !Settings.Validate() )
            {
                IRC.Instance.Send( details.Channel, "{0}: Settings did not validate. Error has been logged", details.Sender.Nickname );
                return;
            }

            try
            {
                Settings.Save();
            }
            catch ( Exception ex )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to save settings. Error has been logged.", details.Sender.Nickname );
                Log.WriteError( "ConfigCommand", "Unable to save settings: {0}", ex.Message );
                return;
            }
        }

        FieldInfo GetConfigScalarField( string configName )
        {
            var settingField = typeof( SettingsXml ).GetField( configName, BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public );

            if ( settingField == null )
                throw new InvalidOperationException( string.Format( "No config field with name {0}", configName ) );

            if ( settingField.GetCustomAttributes( typeof( ConfigHiddenAttribute ), false ).Length > 0 )
                throw new InvalidOperationException( string.Format( "Config field {0} cannot be read or modified", configName ) );

            return settingField;
        }
        IList GetConfigListField( string configName, out Type listType )
        {
            var settingField = GetConfigScalarField( configName );

            object value = settingField.GetValue( Settings.Current );

            IList listValue = value as IList;

            if ( listValue == null )
                throw new InvalidOperationException( string.Format( "Config field {0} is not a list", configName ) );

            listType = settingField.FieldType.GetGenericArguments()[ 0 ];

            return listValue;
        }

        string GetConfigValue( string configName )
        {
            var settingField = GetConfigScalarField( configName );

            object value = settingField.GetValue( Settings.Current );

            if ( value == null )
                throw new InvalidOperationException( string.Format( "Config field {0} has no value", configName ) );

            IEnumerable enumValue = value as IEnumerable;
            if ( enumValue != null )
            {
                return string.Join( ", ", enumValue.Cast<object>() );
            }

            return value.ToString();
        }
        void SetScalarConfigValue( string configName, string inputValue )
        {
            var settingField = GetConfigScalarField( configName );

            object convertedValue = null;

            try
            {
                convertedValue = Convert.ChangeType( inputValue, settingField.FieldType );
            }
            catch ( Exception ex )
            {
                throw new InvalidOperationException( string.Format( "Value for config field {0} could not be converted: {1}", configName, ex.Message ), ex );
            }

            try
            {
                settingField.SetValue( Settings.Current, convertedValue );
            }
            catch ( Exception ex )
            {
                throw new InvalidOperationException( string.Format( "Value for config field {0} could not be set: {1}", configName, ex.Message ), ex );
            }
        }

        void AddListConfigValue( string configName, string inputValue )
        {
            Type listType;
            IList listValue = GetConfigListField( configName, out listType );

            object convertedValue = null;
            try
            {
                convertedValue = Convert.ChangeType( inputValue, listType );
            }
            catch ( Exception ex )
            {
                throw new InvalidOperationException( string.Format( "List value for config field {0} could not be converted: {1}", configName, ex.Message ), ex );
            }

            listValue.Add( convertedValue );
        }
        void RemoveListConfigValue( string configName, string inputValue )
        {
            Type listType;
            IList listValue = GetConfigListField( configName, out listType );

            object convertedValue = null;
            try
            {
                convertedValue = Convert.ChangeType( inputValue, listType );
            }
            catch ( Exception ex )
            {
                throw new InvalidOperationException( string.Format( "List value for config field {0} could not be converted: {1}", configName, ex.Message ), ex );
            }

            listValue.Remove( convertedValue );
        }
        void ClearListConfigValue( string configName )
        {
            Type listType;
            IList listValue = GetConfigListField( configName, out listType );

            listValue.Clear();
        }
    }

    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false )]
    sealed class ConfigHiddenAttribute : Attribute
    {
    }
}
