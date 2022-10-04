using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using Debug = UnityEngine.Debug;

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
            var credential = GoogleCredential.FromJson(SoundShoutSettings.Settings.clientSecretJsonData).CreateScoped(SheetsService.Scope.Spreadsheets);
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME,
            });
        }

        internal static void OpenSpreadSheetInBrowser() => Process.Start($"https://docs.google.com/spreadsheets/d/{SpreedSheetURL}");

        internal static void UpdateAudioSpreadSheet()
        {
            var audioRefs = AssetUtilities.GetAllAudioReferences();
            FetchSpreadsheetChanges(audioRefs);
            
            ClearAllSheetsRequest();
            
            UploadLocalAudioReferenceChanges(audioRefs);

            Debug.Log("AudioReferenceExporter: All AudioReference is up-to-date");
        }

        internal static void FetchSpreadsheetChangesUIButton()
        {
            FetchSpreadsheetChanges(AssetUtilities.GetAllAudioReferences());
            Debug.Log("Fetched all changes!");
        }

        private static void FetchSpreadsheetChanges(IReadOnlyCollection<AudioReference> audioReferences)
        {
            var data = GetSheetData(SpreedSheetURL);
            var remoteSheets = GetSpreadsheetTabsList(data);
            UpdateLocalEvents(audioReferences, remoteSheets);
        }
        
        private static void UpdateLocalEvents(IReadOnlyCollection<AudioReference> localAudioReferences, IReadOnlyList<string> remoteSheets)
        {
            LocalAssetDeleter localAssetDeleter = new LocalAssetDeleter();
            
            try
            {
                AssetDatabase.DisallowAutoRefresh();
                Dictionary<string, AudioReference> duplicationDictionary = new Dictionary<string, AudioReference>();
                List<AudioReference> newAudioRefsList = new List<AudioReference>(10);
                for (int sheetIndex = 0; sheetIndex < remoteSheets.Count; sheetIndex++)
                {
                    var range = $"{remoteSheets[sheetIndex]}!{STANDARD_RANGE}";
                    var request = Service.Spreadsheets.Values.Get(SpreedSheetURL, range);

                    ValueRange response = request.Execute();
                    IList<IList<object>> values = response.Values;
                    if (values is {Count: > 0})
                    {
                        // Go through each row and their data
                        foreach (var row in values)
                        {
                            string eventName = $"{(string)row[(int)UsedRows.EventName]}";
                            if (duplicationDictionary.ContainsKey(eventName))
                            {
                                Debug.LogError($"AudioReference Duplication detected in spreadsheet: {eventName}", duplicationDictionary[eventName]);
                                continue;
                            }
                            
                            bool is3D = (string)row[(int)UsedRows.Is3D] == "3D";
                            bool isLooping = (string)row[(int)UsedRows.Looping] == "Loop";
                            string parameters = (string)row[(int)UsedRows.Parameters];
                            string description = (string)row[(int)UsedRows.Description];
                            string feedback = (string)row[(int)UsedRows.Feedback];

                            string enumString = (string)row[(int)UsedRows.ImplementStatus];
                            bool couldParseStatusText = Enum.TryParse(enumString, true, out AudioReference.ImplementationStatus parsedImplementationStatus);
                            if (!couldParseStatusText)
                            {
                                Debug.LogError($"Spreadsheet event \"{eventName}\" has a status value \"{enumString}\" which can't be parsed.");
                                continue;
                            }

                            bool assetExistLocally = AssetUtilities.DoesAudioReferenceExist(eventName);
                            if (parsedImplementationStatus == AudioReference.ImplementationStatus.Delete)
                            {
                                if (assetExistLocally)
                                    localAssetDeleter.AddAssetPath(AssetUtilities.GetProjectPathForEventAsset(eventName));
                                
                                continue;
                            }
                            
                            AudioReference audioRef;
                            if (assetExistLocally)
                            {
                                audioRef = AssetUtilities.GetAudioReferenceAtPath(eventName);
                                AudioReferenceAssetEditor.ApplyChanges(audioRef, is3D, isLooping, parameters, description, feedback, parsedImplementationStatus);
                            }
                            else
                            {
                                audioRef = AssetUtilities.CreateNewAudioReferenceAsset(eventName);
                                AssetUtilities.ConfigureAudioReference(audioRef, is3D, isLooping, parameters, description, feedback, parsedImplementationStatus);
                                newAudioRefsList.Add(audioRef);
                            }
                            
                            duplicationDictionary.Add(eventName, audioRef);
                        }
                    }
                    else
                    {
                        Debug.Log($"No data was found in tab: \"{remoteSheets[sheetIndex]}\"");
                    }
                }

                AssetDatabase.AllowAutoRefresh();

                int currentSize = localAudioReferences.Count;
                int newSize = currentSize + newAudioRefsList.Count;
                if (currentSize != newSize)
                {
                    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
                    for (int i = 0; i < newAudioRefsList.Count; i++)
                    {
                        stringBuilder.Append("-");
                        stringBuilder.AppendLine(newAudioRefsList[i].eventName);
                    }
                    Debug.Log($"<color=cyan>Created {newAudioRefsList.Count} new audio references (Click for info)</color>\n{stringBuilder}");
                    
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                
                localAssetDeleter.DeleteAssets();
            }
            catch (Exception)
            {
                AssetDatabase.AllowAutoRefresh();
                throw;
            }
        }

        private static void UploadLocalAudioReferenceChanges(AudioReference[] audioReferences)
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
                Values = new List<IList<object>> {new object[] {
                    $"Updated\n{DateTime.Now.ToString("G", CultureInfo.InvariantCulture)}", 
                    DateTime.Now.Subtract(new DateTime(1899,12,30)).TotalDays}},
                Range = LAST_UPDATED_RANGE
            };

            data.Add(updateText);

            
            IList<Request> createdTabsList = CreateMissingSheetTabs(SpreedSheetURL, categories);
            bool hasCreatedNewTabs = createdTabsList != null;
            if (hasCreatedNewTabs)
            {
                var spreadsheet = GetSheetData(SpreedSheetURL);
                var batchUpdateNewTabsRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>()
                };
            
                foreach (var sheet in createdTabsList)
                {
                    string tabTitle = sheet.AddSheet.Properties.Title;
                    if (tabTitle == OVERVIEW_TAB)
                        continue;

                    foreach (var sheetData in spreadsheet.Sheets)
                    {
                        if (sheetData.Properties.Title == tabTitle)
                        {
                            int sheetID = (int) sheetData.Properties.SheetId;
                            SheetFormatting.AddEmptyConditionalFormattingRequests(ref batchUpdateNewTabsRequest, sheetID);
                            break;
                        }
                    }
                }
            
                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateNewTabsRequest, SpreedSheetURL);
                batchUpdateRequest.Execute();
            }
            
            BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest {ValueInputOption = "USER_ENTERED", Data = data};

            SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(requestBody, SpreedSheetURL);
            request.Execute();

            if (hasCreatedNewTabs)
            {
                ApplyFormattingToSpreadSheet();
            }
        }

        private static void ClearAllSheetsRequest()
        {
            Spreadsheet data = GetSheetData(SpreedSheetURL);
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


        private static IList<Request> CreateMissingSheetTabs(string spreadsheetURL, Dictionary<string, int> categories)
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

                return batchUpdateSpreadsheetRequest.Requests;
            }

            return null;
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

        internal static void ApplyFormattingToSpreadSheet()
        {
            var spreadsheetData = GetSheetData(SpreedSheetURL);
            IList<ValueRange> headerTextValueRanges = new List<ValueRange>();
            foreach (var sheet in spreadsheetData.Sheets)
            {
                string tabTitle = sheet.Properties.Title;
                if (tabTitle == OVERVIEW_TAB)
                    continue;

                headerTextValueRanges.Add(SheetFormatting.GetHeaderTextValueRange(tabTitle));
            }

            // Add header column text
            BatchUpdateValuesRequest updateHeaderTextRequest = new BatchUpdateValuesRequest {ValueInputOption = "USER_ENTERED", Data = headerTextValueRanges};
            SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(updateHeaderTextRequest, SpreedSheetURL); 
            request.Execute();
            
            // Apply formatting
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest { Requests = new List<Request>() };
            AddFormattingRequests(ref batchUpdateSpreadsheetRequest, spreadsheetData);

            var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, SpreedSheetURL); 
            batchUpdateRequest.Execute();
        }

        private static void AddFormattingRequests(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, Spreadsheet spreadsheetData)
        {
            foreach (var sheet in spreadsheetData.Sheets)
            {
                string tabTitle = sheet.Properties.Title;
                if (tabTitle == OVERVIEW_TAB)
                    continue;

                int sheetID = (int) sheet.Properties.SheetId;
                SheetFormatting.ApplyHeaderFormatting(ref batchUpdateSpreadsheetRequest, sheetID);
                SheetFormatting.ApplyRowFormatting(ref batchUpdateSpreadsheetRequest, sheetID);
            }
        }
    }
}