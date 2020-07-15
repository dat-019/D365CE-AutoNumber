using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoNumberGeneration
{
    /// <summary>
    /// Plugin to create a auto number id for a new record by configured Auto Number string format
    /// </summary>
    public class Autonumber : PluginBase
    {
        public Autonumber() : base(typeof(Autonumber)) { }

        protected override void ExecuteCrmPlugin(LocalPluginContext localcontext)
        {
            var context = localcontext.PluginExecutionContext;
            var service = localcontext.OrganizationService;

            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                var entity = (Entity)context.InputParameters["Target"];

                if (!string.IsNullOrEmpty(entity.LogicalName))
                {
                    var listAutoNumber = GetCollectionAutoNumber(entity, service);
                    if (listAutoNumber != null && listAutoNumber.Entities.Count > 0)
                    {
                        var item = listAutoNumber.Entities[0];
                        bupa_autonumber autoNumberRecord = item.ToEntity<bupa_autonumber>();
                        var autoNumberFieldName = autoNumberRecord.bupa_FieldName;
                        if (!entity.Contains(autoNumberFieldName))
                        {
                            int lastNumber = 1;
                            bool success = false;
                            string autoNumberFormat = autoNumberRecord.bupa_AutoNumberStringFormat;
                            int numberOfRetry = 0;

                            while (!success)
                            {
                                numberOfRetry += 1;
                                localcontext.Trace("numberOfRetry:: " + numberOfRetry);
                                if (numberOfRetry == 5)
                                {
                                    throw new InvalidPluginExecutionException("A mismatched row version of the auto number record caused the request to fail. Please try again later");
                                }

                                if (autoNumberRecord.bupa_NextNumber != null)
                                {
                                    lastNumber = autoNumberRecord.bupa_NextNumber.Value;
                                }
                                autoNumberFormat = autoNumberRecord.bupa_AutoNumberStringFormat;
                                //autoNumberRecord.bupa_NextNumber = lastNumber + 1;

                                // Create an in-memory auto number object from the retrieved auto number.
                                var updatedAutonumber = new Entity()
                                {
                                    LogicalName = autoNumberRecord.LogicalName,
                                    Id = autoNumberRecord.Id,
                                    RowVersion = autoNumberRecord.RowVersion
                                };
                                updatedAutonumber["bupa_nextnumber"] = lastNumber + 1;
                                UpdateRequest updateRequest = new UpdateRequest
                                {
                                    Target = updatedAutonumber,
                                    ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
                                };
                                try
                                {
                                    service.Execute(updateRequest);
                                    success = true;
                                }
                                catch (FaultException<OrganizationServiceFault> e)
                                {
                                    if (e.Detail.ErrorCode == ErrorCodes.ConcurrencyVersionMismatch)
                                    {
                                        success = false;
                                        autoNumberRecord = GetAutoNumberRecord(entity, service);
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }

                            entity[autoNumberFieldName] = GetFormatAutoNumber(autoNumberFormat, lastNumber);
                        }
                    }
                }
            }
        }

        #region private methods

        private static string GetFormatAutoNumber(string autoNumberFormatStr, int lastNumber)
        {
            var tempStringFormat = autoNumberFormatStr;
            if (tempStringFormat.Contains("{PAD") && tempStringFormat.Contains("{n}}"))
            {
                var tempLastNumber = lastNumber.ToString(CultureInfo.InvariantCulture);

                var splitCharater = Regex.Match(tempStringFormat, @"\{PAD.*{n}}"); //get character from "{PAD" to "{n}}"
                var padformat = Regex.Match(splitCharater.Value, @"\d+"); //get number format between "{PAD" and "{n}}"

                int padFormatVal;
                int.TryParse(padformat.Value, out padFormatVal);
                var formatNumber = tempLastNumber.PadLeft(padFormatVal, '0');
                tempStringFormat = Regex.Replace(tempStringFormat, @"\{PAD.*{n}}", formatNumber, RegexOptions.IgnoreCase);
            }
            else
            {
                tempStringFormat = tempStringFormat.Replace("{n}", lastNumber.ToString(CultureInfo.InvariantCulture));
            }

            return tempStringFormat;
        }

        /// <summary>
        /// This method returns the AutoNumber Entity collection
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        private static EntityCollection GetCollectionAutoNumber(Entity entity, IOrganizationService service)
        {
            var entityName = entity.LogicalName;
            string fetchXml = string.Format(
                @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>+
                                    <entity name='{1}'>
                                        <attribute name='{2}'/>
                                        <attribute name='{3}'/>
                                        <attribute name='{4}'/>
                                        <attribute name='{5}'/>
                                        <attribute name='{6}'/>
                                        <attribute name='{7}'/>
                                        <filter type='and'>
                                            <condition attribute='{8}' operator='eq' value='0' />
                                            <condition attribute='{9}' operator='eq' value='{0}' />
                                        </filter>
                                    </entity>
                    </fetch>",
                entityName, bupa_autonumber.EntityLogicalName, FieldName.AutoNumber.AutoNumberId, FieldName.AutoNumber.AutoNumberName,
                FieldName.AutoNumber.AutoNumberFieldName, FieldName.AutoNumber.AutoNumberStringFormat, FieldName.AutoNumber.AutoNumberNextNumber,
                FieldName.AutoNumber.AutoNumberExecuteOptionSetTextIs, FieldName.AutoNumber.AutoNumberStateCode, FieldName.AutoNumber.AutoNumberEntityName);

            var fetch = new Microsoft.Xrm.Sdk.Query.FetchExpression(fetchXml);

            //execute the FetchXML
            EntityCollection results = service.RetrieveMultiple(fetch);
            return results;
        }

        private static bupa_autonumber GetAutoNumberRecord(Entity entity, IOrganizationService service)
        {
            var entityName = entity.LogicalName;
            string fetchXml = string.Format(
                @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>+
                                    <entity name='{1}'>
                                        <attribute name='{2}'/>
                                        <attribute name='{3}'/>
                                        <attribute name='{4}'/>
                                        <attribute name='{5}'/>
                                        <attribute name='{6}'/>
                                        <attribute name='{7}'/>
                                        <filter type='and'>
                                            <condition attribute='{8}' operator='eq' value='0' />
                                            <condition attribute='{9}' operator='eq' value='{0}' />
                                        </filter>
                                    </entity>
                    </fetch>",
                entityName, bupa_autonumber.EntityLogicalName, FieldName.AutoNumber.AutoNumberId, FieldName.AutoNumber.AutoNumberName,
                FieldName.AutoNumber.AutoNumberFieldName, FieldName.AutoNumber.AutoNumberStringFormat, FieldName.AutoNumber.AutoNumberNextNumber,
                FieldName.AutoNumber.AutoNumberExecuteOptionSetTextIs, FieldName.AutoNumber.AutoNumberStateCode, FieldName.AutoNumber.AutoNumberEntityName);

            var fetch = new Microsoft.Xrm.Sdk.Query.FetchExpression(fetchXml);

            //execute the FetchXML
            EntityCollection results = service.RetrieveMultiple(fetch);
            if (results != null && results.Entities.Count > 0)
            {
                var item = results.Entities[0];
                return item.ToEntity<bupa_autonumber>();
            }

            return null;
        }

        #endregion
    }
}
