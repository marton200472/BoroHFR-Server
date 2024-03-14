namespace BoroHFR.Helpers
{
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    internal class SwaggerControllerFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var item in swaggerDoc.Paths.ToArray())
            {
                if(!item.Key.ToLower().Contains("/api/"))
                {
                    swaggerDoc.Paths.Remove(item.Key);
                }
            }
        }
    }
}
