using PropertyChanged;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace SWTools.Core {
    /// <summary>
    /// 创意工坊物品物品列表 (容器) - 解析服务
    /// </summary>
    public partial class ItemList : ObservableCollection<Item> {
        // 解析全部队列中物品
        public async Task ParseAll() {
            if (!ConfigManager.Config.NoCache) await ParseWithCache();
            await ParseWithRequest();
        }

        // 缓存解析
        private async Task ParseWithCache() {
            _ = ConvertParseState(Item.EParseState.Pending, Item.EParseState.Handling);
            await Task.Delay(500); // 仪式感
            for (var i = 0; i < Count; i++) {
                if (this[i].ParseState == Item.EParseState.Handling) {
                    if (Cache.Parse.Get(this[i].ItemId) != null) {
                        this[i] = Cache.Parse.Get(this[i].ItemId)!;
                    }
                }
            }
            _ = ConvertParseState(Item.EParseState.Handling, Item.EParseState.Pending);
        }

        // 联网解析
        private async Task ParseWithRequest() {
            if (await ParseWithGetPublishedFileDetails()) return;
            // 重试
            ConvertParseState(Item.EParseState.Failed, Item.EParseState.Pending);
            await ParseWithSwDownloader();
        }

        private async Task<bool> ParseWithSwDownloader() {
            // 请求 API
            var items = ConvertParseState(Item.EParseState.Pending, Item.EParseState.Handling);
            if (items.Count == 0) return false;
            var responses = await API.SwDownloader.Request(items);
            // 处理回复
            if (responses == null || responses.Length == 0) {
                LogManager.Log.Error("Failed to parse items with SwDownloader: Empty response");
                return false;
            } else {
                foreach (var response in responses) {
                    if (!Contains(response.publishedfileid!)) continue;
                    this[FindIndex(response.publishedfileid!)].ParseFrom(response);
                }
            }
            _ = ConvertParseState(Item.EParseState.Handling, Item.EParseState.Failed);
            return IsAllParsed();
        }

        private async Task<bool> ParseWithGetPublishedFileDetails() {
            // 请求 API
            var items = ConvertParseState(Item.EParseState.Pending, Item.EParseState.Handling);
            if (items.Count == 0) return false;
            var response = await API.GetPublishedFileDetails.Request(items);
            // 处理回复
            if (response == null || response.resultcount == 0 || response.publishedfiledetails == null) {
                LogManager.Log.Error("Failed to parse items with GetPublishedFileDetails: Empty response");
                return false;
            } else {
                foreach (var detail in response.publishedfiledetails) {
                    if (detail.publishedfileid == null) continue;
                    if (!Contains(detail.publishedfileid!)) continue;
                    this[FindIndex(detail.publishedfileid!)].ParseFrom(detail);
                }
            }
            _ = ConvertParseState(Item.EParseState.Handling, Item.EParseState.Failed);
            return IsAllParsed();
        }

        // 转换解析状态
        private List<string> ConvertParseState(Item.EParseState from, Item.EParseState to) {
            List<string> items = [];
            foreach (var item in this) {
                if (item.ParseState == from) {
                    item.ParseState = to;
                    items.Add(item.ItemId);
                }
            }
            return items;
        }

        // 是否全部解析成功
        private bool IsAllParsed() {
            foreach (var item in this) {
                if (item.ParseState != Item.EParseState.Done) return false;
            }
            return true;
        }
    }
}