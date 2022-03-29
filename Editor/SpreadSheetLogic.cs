using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace SoundShout.Editor
{
    public static class SpreadSheetLogic
    {
        private const string APPLICATION_NAME = "TOEM";
        private static string SpreedSheetURL => SoundShoutSettings.Settings.spreadsheetURL;

        private static SheetsService service;
        private static SheetsService Service => service ?? (service = GetSheetsService());

        private enum UsedRows { EventName = 0, Is3D = 1, Looping = 2, Parameters = 3, Description = 4, Feedback = 5, ImplementStatus = 6 }

        private const string OVERVIEW_TAB = "~Overview";
        private const string LAST_UPDATED_RANGE = OVERVIEW_TAB + "!H1";
        private const string START_RANGE = "A2";
        private const string END_RANGE = "G";
        private const string STANDARD_RANGE = START_RANGE + ":" + END_RANGE;

        private static Spreadsheet GetSheetData(string spreadSheetUrl)
        {
            return Service.Spreadsheets.Get(spreadSheetUrl).Execute();
        }

        private static List<string> GetSpreadsheetTabsList(Spreadsheet ss, bool includeOverview = false)
        {
            List<string> sheetTabs = new List<string>();
            foreach (Sheet sheet in ss.Sheets)
            {
                if (!includeOverview && sheet.Properties.Title == OVERVIEW_TAB)
                {
                    continue;
                }

                sheetTabs.Add(sheet.Properties.Title);
            }

            return sheetTabs;
        }

        private static SheetsService GetSheetsService()
        {
            GoogleCredential credential;
            const string secretsPath = SoundShoutPaths.CLIENT_SECRET_PATH;
            using (var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME,
            });
        }

        internal static void OpenSpreadSheetInBrowser() => System.Diagnostics.Process.Start($"https://docs.google.com/spreadsheets/d/{SpreedSheetURL}");

        internal static void UpdateAudioSpreadSheet()
        {
            var audioRefs = AssetUtilities.GetAllAudioReferences();

            FetchSpreadsheetChanges();

            ClearAllSheetsRequest();

            UploadLocalAudioReferenceChanges(ref audioRefs);

            Debug.Log("AudioReferenceExporter: All AudioReference is up-to-date");
        }
        
        internal static void FetchSpreadsheetChanges()
        {
            var data = GetSheetData(SpreedSheetURL);
            var audioRefs = AssetUtilities.GetAllAudioReferences();
            var sheetTabs = GetSpreadsheetTabsList(data);
            ReadEntries(ref audioRefs, ref sheetTabs);
        }

        private static void ReadEntries(ref AudioReference[] audioReferences, ref List<string> sheets)
        {
            List<AudioReference> newAudioRefsList = new List<AudioReference>(10);
            for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                var range = $"{sheets[sheetIndex]}!{STANDARD_RANGE}";
                var request = Service.Spreadsheets.Values.Get(SpreedSheetURL, range);

                ValueRange response = request.Execute();
                IList<IList<object>> values = response.Values;
                if (values != null && values.Count > 0)
                {
                    // Go through each row and their data
                    foreach (var row in values)
                    {
                        string eventName = $"{(string) row[(int) UsedRows.EventName]}";
                        bool is3D = (string) row[(int) UsedRows.Is3D] == "3D";
                        bool isLooping = (string) row[(int) UsedRows.Looping] == "Loop";
                        string parameters = (string) row[(int) UsedRows.Parameters];
                        string description = (string) row[(int) UsedRows.Description];
                        string feedback = (string) row[(int) UsedRows.Feedback];

                        string enumString = (string) row[(int) UsedRows.ImplementStatus];
                        var parsedImplementationStatus = (AudioReference.ImplementationStatus) 
                            Enum.Parse(typeof(AudioReference.ImplementationStatus), enumString);

                        if (parsedImplementationStatus == AudioReference.ImplementationStatus.Delete)
                        {
                            Debug.Log($"Skipped creating audio reference for \"{eventName}\" as it's marked as Delete!");
                            continue;
                        }
                        
                        string fullEventName = $"event:/{eventName}";
                        bool doesAudioReferenceExist = AssetUtilities.DoesAudioReferenceExist(fullEventName);
                        if (doesAudioReferenceExist)
                        {
                            var audioRef = AssetUtilities.GetAudioReferenceAtPath(eventName);
                            AudioReferenceAssetEditor.ApplyChanges(audioRef, is3D, isLooping, parameters, description, feedback, parsedImplementationStatus);
                            break;
                        }
                        else
                        {
                            var newSound = AssetUtilities.CreateNewAudioReferenceAsset(eventName);
                            AssetUtilities.ConfigureAudioReference(newSound, is3D, isLooping, parameters, description, feedback, parsedImplementationStatus);
                            newAudioRefsList.Add(newSound);
                        }
                    }
                }
                else
                {
                    Debug.Log($"No data was found in tab: \"{sheets[sheetIndex]}\"");
                }
            }

            int currentSize = audioReferences.Length;
            int newSize = currentSize + newAudioRefsList.Count;
            if (currentSize < newSize)
            {
                newAudioRefsList.AddRange(audioReferences);
                audioReferences = newAudioRefsList.ToArray();
                
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        private static void UploadLocalAudioReferenceChanges(ref AudioReference[] audioReferences)
        {
            Dictionary<string, int> categories = new Dictionary<string, int>();

            List<ValueRange> data = new List<ValueRange>();
            foreach (var audioRef in audioReferences)
            {
                // If category don't exist, create it
                if (!categories.ContainsKey(audioRef.category))
                {
                    // indention starts at index 2 in the spreadsheet
                    categories.Add(audioRef.category, 2);
                }
                else
                {
                    // add indention per entry
                    categories[audioRef.category]++;
                }

                var objectList = new List<object>
                {
                    audioRef.eventName,
                    audioRef.is3D ? "3D" : "2D",
                    audioRef.looping ? "Loop" : "OneShot",
                    audioRef.parameters,
                    audioRef.description,
                    audioRef.feedback,
                    audioRef.implementationStatus.ToString()
                };

                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> {objectList},
                    Range = $"{audioRef.category}!A{categories[audioRef.category]}"
                };

                data.Add(valueRange);
            }

            // Last Updated Text
            var updateText = new ValueRange
            {
                Values = new List<IList<object>> {new object[] {"Updated\n" + DateTime.Now.ToString("g", CultureInfo.InvariantCulture)}},
                Range = LAST_UPDATED_RANGE
            };

            data.Add(updateText);

            bool hasCreatedNewTabs = CreateMissingSheetTabs(SpreedSheetURL, categories);

            BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest {ValueInputOption = "USER_ENTERED", Data = data};

            SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(requestBody, SpreedSheetURL);
            request.Execute();

            if (hasCreatedNewTabs)
            {
                var spreadsheet = GetSheetData(SpreedSheetURL);
                var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>()
                };

                foreach (var sheet in spreadsheet.Sheets)
                {
                    string tabTitle = sheet.Properties.Title;
                    if (tabTitle == OVERVIEW_TAB)
                        continue;

                    // ReSharper disable once PossibleInvalidOperationException
                    int sheetID = (int) sheet.Properties.SheetId;
                    SheetsFormatting.AddEmptyConditionalFormattingRequests(ref batchUpdateSpreadsheetRequest, sheetID);
                }

                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, SpreedSheetURL);
                batchUpdateRequest.Execute();
            }

            ApplyFormattingToSpreadSheet();
        }

        private static void ClearAllSheetsRequest()
        {
            var data = GetSheetData(SpreedSheetURL);
            var sheets = GetSpreadsheetTabsList(data);
            List<string> ranges = new List<string>();
            foreach (var sheetTab in sheets)
            {
                ranges.Add($"{sheetTab}!{STANDARD_RANGE}");
            }

            BatchClearValuesRequest requestBody = new BatchClearValuesRequest {Ranges = ranges};

            SpreadsheetsResource.ValuesResource.BatchClearRequest request = Service.Spreadsheets.Values.BatchClear(requestBody, SpreedSheetURL);
            request.Execute();
        }

        private static bool CreateMissingSheetTabs(string spreadsheetURL, Dictionary<string, int> categories)
        {
            var data = GetSheetData(spreadsheetURL);
            var existingTabs = GetSpreadsheetTabsList(data, true);

            BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>()
            };


            if (!existingTabs.Contains(OVERVIEW_TAB))
            {
                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = CreateNewAddOverviewTabRequest()
                });
            }

            foreach (var category in categories)
            {
                // Don't duplicate existing tabs
                if (existingTabs.Contains(category.Key))
                    continue;

                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = CreateNewAddTabRequest(category.Key)
                });
            }

            bool shouldUpdate = batchUpdateSpreadsheetRequest.Requests.Count > 0;
            if (shouldUpdate)
            {
                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetURL);
                batchUpdateRequest.Execute();

                return true;
            }

            return false;
        }

        private static AddSheetRequest CreateNewAddOverviewTabRequest()
        {
            return new AddSheetRequest
            {
                Properties = new SheetProperties
                {
                    Index = 0,
                    Title = OVERVIEW_TAB,
                    GridProperties = new GridProperties
                    {
                        ColumnCount = 8,
                        RowCount = 12,
                    }
                }
            };
        }

        private static AddSheetRequest CreateNewAddTabRequest(string title)
        {
            return new AddSheetRequest
            {
                Properties = new SheetProperties
                {
                    Title = title,
                    GridProperties = new GridProperties
                    {
                        FrozenRowCount = 1,
                        ColumnCount = 7,
                        RowCount = 100,
                    }
                }
            };
        }

        public static void ApplyFormattingToSpreadSheet()
        {
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>()
            };

            var data = GetSheetData(SpreedSheetURL);
            var headerTextValueRanges = new List<ValueRange>();

            foreach (var sheet in data.Sheets)
            {
                string tabTitle = sheet.Properties.Title;
                if (tabTitle == OVERVIEW_TAB)
                    continue;

                // ReSharper disable once PossibleInvalidOperationException
                int sheetID = (int) sheet.Properties.SheetId;
                SheetsFormatting.ApplyHeaderFormatting(ref batchUpdateSpreadsheetRequest, sheetID);
                SheetsFormatting.ApplyRowFormatting(ref batchUpdateSpreadsheetRequest, sheetID);

                headerTextValueRanges.Add(SheetsFormatting.GetSetHeaderTextUpdateRequest(tabTitle));
            }

            // Apply formatting
            if (batchUpdateSpreadsheetRequest.Requests.Count > 0)
            {
                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, SpreedSheetURL);
                batchUpdateRequest.Execute();
            }

            // Apply header text
            if (headerTextValueRanges.Count > 0)
            {
                BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest {ValueInputOption = "USER_ENTERED", Data = headerTextValueRanges};
                SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(requestBody, SpreedSheetURL);
                request.Execute();
            }
        }
    }
}