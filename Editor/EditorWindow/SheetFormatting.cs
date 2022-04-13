using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Color = Google.Apis.Sheets.v4.Data.Color;

namespace SoundShout.Editor
{
    public static class SheetFormatting
    {
        #region Header

        internal static void ApplyHeaderFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            // Auto resize all headers
            var autoResizeDimensionsRequest = GetAutoResizeDimensionsRequest(sheetID);
            batchUpdateSpreadsheetRequest.Requests.Add(new Request {AutoResizeDimensions = autoResizeDimensionsRequest});

            //create the update request for cells from the first row
            var repeatCell = new RepeatCellRequest
            {
                Range = GetHeaderGridRange(sheetID),
                Cell = new CellData
                {
                    UserEnteredFormat = GetHeaderCellFormat()
                },
                Fields = "UserEnteredFormat(BackgroundColor,TextFormat,HorizontalAlignment)"
            };
            batchUpdateSpreadsheetRequest.Requests.Add(  new Request{UpdateDimensionProperties = HeaderDimensions(sheetID)} );
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {RepeatCell = repeatCell});
        }

        private static AutoResizeDimensionsRequest GetAutoResizeDimensionsRequest(int sheetID)
        {
            return new AutoResizeDimensionsRequest
            {
                Dimensions = new DimensionRange
                {
                    SheetId = sheetID,
                    Dimension = "COLUMNS"
                }
            };
        }
        
        internal static ValueRange GetHeaderTextValueRange(string sheetTabName)
        {
            var textPerCell = new List<object>
            {
                "Event Name",
                "Is 3D",
                "Looping?",
                "Parameters",
                "Description",
                "Feedback",
                "Status"
            };
            
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> {textPerCell},
                Range = $"{sheetTabName}!A1"
            };

            
            return valueRange;
        }

        private static UpdateDimensionPropertiesRequest HeaderDimensions(int sheetID) =>
            new UpdateDimensionPropertiesRequest{
                Range = new DimensionRange
                {
                    SheetId = sheetID,
                    Dimension = "ROWS",
                    StartIndex = 0,
                    EndIndex = 1

                },
                Properties = new DimensionProperties{
                    PixelSize = 60
                },
                Fields = "PixelSize"
            };

        private static CellFormat GetHeaderCellFormat()
        {
            return new CellFormat{
                BackgroundColor = new Color
                {
                    Red = 0.047f,
                    Blue = 0.204f,
                    Green = 0.239f,
                    Alpha = 1
                },
                TextFormat = new TextFormat
                {
                    FontSize = 12,
                    
                    ForegroundColor = new Color
                    {
                        Blue = .8f,
                        Red = .8f,
                        Green = .9f,
                        Alpha = 1
                    }
                },
                HorizontalAlignment = "Left"
            };
        }

        
        private static GridRange GetHeaderGridRange(int sheetId)
        {
            return new GridRange
            {
                SheetId = sheetId,
                EndRowIndex = 1
                //StartRowIndex = 0,
                //StartColumnIndex = 0, // Leaving these out will make the whole row bolded!
                //EndColumnIndex = 6,
            };
        }

        #endregion
        
        #region Rows

        internal static void AddEmptyConditionalFormattingRequests(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            foreach (AudioReference.ImplementationStatus enumValue in Enum.GetValues(typeof(AudioReference.ImplementationStatus)))
            {
                batchUpdateSpreadsheetRequest.Requests.Add( new Request { AddConditionalFormatRule = new AddConditionalFormatRuleRequest { Rule = GetConditionFormatRule(sheetID, enumValue, new Color()) } });
            }
        }
        
        public static void ApplyRowFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetImplementationStatusValidationRequest(sheetID)});
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetIs3DValidationRequest(sheetID)});
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetIsLoopingValidationRequest(sheetID)});
            
            UpdateImplementationStatusConditionalFormatting(ref batchUpdateSpreadsheetRequest, sheetID);
        }

        private static string[] GetStatusEnumNameArray() { return Enum.GetNames(typeof(AudioReference.ImplementationStatus)); }

        private static SetDataValidationRequest GetImplementationStatusValidationRequest(int sheetID)
        {
            var statusValidation = new SetDataValidationRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetID,
                    StartRowIndex = 1,
                    StartColumnIndex = 6,
                    EndColumnIndex = 7
                },
                Rule = new DataValidationRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "ONE_OF_LIST",
                        Values = new List<ConditionValue>()
                    },
                    Strict = true,
                    ShowCustomUi = true
                }
            };

            // Dynamically fill the condition with values
            var enumNames = GetStatusEnumNameArray();
            for (int i = 0; i < enumNames.Length; i++)
            {
                statusValidation.Rule.Condition.Values.Add(new ConditionValue {UserEnteredValue = enumNames[i]});
            }

            return statusValidation;
        }
        
        private static SetDataValidationRequest GetIs3DValidationRequest(int sheetID)
        {
            var statusValidation = new SetDataValidationRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetID,
                    StartRowIndex = 1,
                    StartColumnIndex = 1,
                    EndColumnIndex = 2
                },
                Rule = new DataValidationRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "ONE_OF_LIST",
                        Values = new List<ConditionValue>
                        {
                            new ConditionValue {UserEnteredValue = "2D"},
                            new ConditionValue {UserEnteredValue = "3D"},
                        }
                    },
                    Strict = true,
                    ShowCustomUi = true
                }
            };

            return statusValidation;
        }

        private static SetDataValidationRequest GetIsLoopingValidationRequest(int sheetID)
        {
            var statusValidation = new SetDataValidationRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetID,
                    StartRowIndex = 1,
                    StartColumnIndex = 2,
                    EndColumnIndex = 3
                },
                Rule = new DataValidationRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "ONE_OF_LIST",
                        Values = new List<ConditionValue>
                        {
                            new ConditionValue {UserEnteredValue = "OneShot"},
                            new ConditionValue {UserEnteredValue = "Loop"},
                        }
                    },
                    Strict = true,
                    ShowCustomUi = true
                }
            };

            return statusValidation;
        }

        private static void UpdateImplementationStatusConditionalFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            var settings = SoundShoutSettings.Settings;

            for (int i = 0; i < settings.statusValidations.Length; i++)
            {
                var statusValidation = settings.statusValidations[i];
                
                batchUpdateSpreadsheetRequest.Requests.Add( new Request
                {
                    UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, statusValidation.implementationStatus, new Color
                    {
                        Red =   statusValidation.color.r,
                        Green = statusValidation.color.g,
                        Blue =  statusValidation.color.b
                    })
                });
            }
        }

        private static UpdateConditionalFormatRuleRequest CreateConditionalFormat(int sheetID, AudioReference.ImplementationStatus status, Color rowColor)
        {
            return new UpdateConditionalFormatRuleRequest
            {
                Index = (int)status,
                Rule = GetConditionFormatRule(sheetID, status, rowColor),
            };
        }

        private static ConditionalFormatRule GetConditionFormatRule(int sheetID, AudioReference.ImplementationStatus status, Color rowColor)
        {
            return new ConditionalFormatRule
            {
                Ranges = new List<GridRange>
                {
                    new GridRange
                    {
                        SheetId = sheetID,
                        StartColumnIndex = 0,
                        StartRowIndex = 1
                    }
                },
                BooleanRule = new BooleanRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "CUSTOM_FORMULA",
                        Values = new List<ConditionValue>
                        {
                            new ConditionValue
                            {
                                UserEnteredValue = $"=$G2=\"{status.ToString()}\""
                            }
                        }
                    },
                    Format = new CellFormat
                    {
                        BackgroundColor = rowColor
                    }
                }
            };
        }
        
        #endregion
    }
}