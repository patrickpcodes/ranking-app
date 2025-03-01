using Microsoft.AspNetCore.Mvc;
using TMDbLib.Client;
using TMDbLib.Objects.Search;
using System.Threading.Tasks;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Collections;

namespace RankingApp.Server.Api.Controllers;

[ApiController]
[Route( "api/[controller]" )]
public class TmdbController : ControllerBase
{
    private readonly TMDbClient _tmdbClient;

    public TmdbController( TMDbClient tmdbClient )
    {
        _tmdbClient = tmdbClient;
    }
    [HttpGet( "collections/{collectionId}" )]
    public async Task<IActionResult> GetCollectionById( int collectionId )
    {
        if( collectionId <= 0 )
        {
            return BadRequest( "Invalid collection ID." );
        }

        Collection collection = await _tmdbClient.GetCollectionAsync( collectionId );

        if( collection != null )
        {
            return Ok( collection );
        }

        return NotFound( "Collection not found." );
    }
    [HttpGet( "search/collections" )]
    public async Task<IActionResult> SearchCollections( [FromQuery] string query )
    {
        if( string.IsNullOrWhiteSpace( query ) )
        {
            return BadRequest( "Query parameter is required." );
        }

        // Search for collections using the TMDbClient
        SearchContainer<SearchCollection> searchResults = await _tmdbClient.SearchCollectionAsync( query );

        if( searchResults.Results.Count > 0 )
        {
            return Ok( searchResults.Results );
        }

        return NotFound( "No collections found." );
    }
}
