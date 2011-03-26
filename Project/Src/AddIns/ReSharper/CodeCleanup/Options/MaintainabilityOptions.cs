﻿//-----------------------------------------------------------------------
// <copyright file="">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. If you cannot locate the  
//   Microsoft Public License, please send an email to dlr@microsoft.com. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
//-----------------------------------------------------------------------

namespace StyleCop.ReSharper.CodeCleanup.Options
{
    #region Using Directives

    using System.ComponentModel;
    using System.Reflection;
    using System.Text;

    #endregion

    /// <summary>
    /// Defines options for SCfR#.
    /// </summary>
    public class MaintainabilityOptions : OptionsBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintainabilityOptions"/> class. 
        /// </summary>
        public MaintainabilityOptions()
        {
            this.InitPropertiesDefaults();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether SA1119StatementMustNotUseUnnecessaryParenthesis.
        /// </summary>
        [DisplayName("1119: Statement Must Not Use Unnecessary Parenthesis")]
        public bool SA1119StatementMustNotUseUnnecessaryParenthesis { get; set; }

        /// <summary>
        /// Gets the name of the analyzer.
        /// </summary>
        protected override string AnalyzerName
        {
            get { return "Microsoft.StyleCop.CSharp.MaintainabilityRules"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a concatenated summary of the current options settings.
        /// </summary>
        /// <returns>
        /// A String of the options.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            var properties = this.GetType().GetProperties();

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(this.GetPropertyDecription(property));
            }

            return sb.ToString();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds a string reperesentation of the property value.
        /// </summary>
        /// <param name="propertyInfo">
        /// THe propertyInof to build the descriotion for.
        /// </param>
        /// <returns>
        /// The string representation.
        /// </returns>
        private string GetPropertyDecription(PropertyInfo propertyInfo)
        {
            var propertyValue = propertyInfo.GetValue(this, null).ToString();

            var propName = string.Empty;
            var propValue = string.Empty;
            var displayNameAttributes = (DisplayNameAttribute[])propertyInfo.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            if (displayNameAttributes.Length == 1)
            {
                propName = displayNameAttributes[0].DisplayName;
            }

            if (propertyInfo.PropertyType == typeof(bool))
            {
                propValue = propertyValue == "True" ? "Yes" : "No";
            }
            else
            {
                var field = propertyInfo.PropertyType.GetField(propertyValue);

                if (field != null)
                {
                    var descriptionAttributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (descriptionAttributes.Length == 1)
                    {
                        propValue = descriptionAttributes[0].Description;
                    }
                }
            }

            return string.Format("{0} = {1}", propName, propValue);
        }

        #endregion
    }
}