using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020488.BusinessLayers;

namespace SV22T1020488.Shop.AppCodes // <--- KIỂM TRA KỸ DÒNG NÀY
{
    public static class SelectListHelper
    {
        public static async Task<List<SelectListItem>> Provinces()
        {
            var list = new List<SelectListItem>()
            {
                new SelectListItem() { Value = "", Text = "-- Chọn Tỉnh/Thành --" }
            };

            var result = await DictionaryDataService.ListProvincesAsync();
            foreach (var item in result)
            {
                list.Add(new SelectListItem()
                {
                    Value = item.ProvinceName,
                    Text = item.ProvinceName
                });
            }
            return list;
        }
    }
}