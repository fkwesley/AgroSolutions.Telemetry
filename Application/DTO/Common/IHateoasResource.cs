namespace Application.DTO.Common
{
    /// <summary>
    /// Interface para recursos que suportam HATEOAS.
    /// 
    /// ?? OBJETIVO:
    /// Define um contrato para que todos os DTOs de resposta possam incluir links HATEOAS.
    /// 
    /// ?? USO:
    /// Implemente esta interface em qualquer DTO que deve retornar links:
    /// public class OrderResponse : IHateoasResource
    /// {
    ///     public List&lt;Link&gt; Links { get; set; } = new();
    /// }
    /// </summary>
    public interface IHateoasResource
    {
        /// <summary>
        /// Lista de links HATEOAS relacionados a este recurso.
        /// Sempre inicializado como lista vazia.
        /// </summary>
        List<Link> Links { get; set; }
    }
}
