using MovieApi.Dtos;
using MovieApi.Models;
using MovieApi.Services;

namespace MovieApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {

        private List<string> _allowedExtenstions = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPosterSize = 1048576;

        private readonly IMoviesService _moviesService;
        private readonly IGenresService _genresService;

        public MoviesController(IMoviesService moviesService, IGenresService genresService)
        {
            _moviesService = moviesService;
            _genresService = genresService;
        }


        //[HttpGet]
        //public async Task<IActionResult> GetAllAsync() //// first option(return object of genre)
        //{
        //    var movie = await _context.Movies.Include(g => g.Genre).ToListAsync();

        //    return Ok(movie);
        //}



        [HttpGet]
        public async Task<IActionResult> GetAllAsync()  ///// to return genre name 
        {
            var movie = await _moviesService.GetAll();
            ///TODO: mapping movies to DtoMovies

            return Ok(movie);
        }


        [HttpGet(template: "{id}")]
        public async Task<IActionResult> GetByIdAsync(int id) 
        {
            var movie = await _moviesService.GetById(id); //// we cant use findBy(id) because there conflict with include syntax


            if (movie == null)     
               return NotFound(value: $"no Movie is found with Id: {id}");

            //// map movie to MovieDetailsDto to can get GenreName if you want.
            //var dto = new MovieDetailsDto 
            //{
            //    Id = movie.Id,
            //    Description = movie.Description,
            //    GenreName = movie.Genre.Name,
            //    Rate = movie.Rate,
            //    Title = movie.Title,
            //    Year = movie.Year,
            //    Poster = movie.Poster,

            //};

            return Ok(movie);
            
        }

        [HttpGet(template: "GetByGenreId")]
        public async Task<IActionResult> GetMovieByGenreId(byte genreId)
        {
            var movie = await _moviesService.GetAll(genreId);
            ///TODO: mapping movies to DtoMovies

            if (movie == null)
                return NotFound(value: $"no Movie is found with this GenreId: {genreId}");

            return Ok(movie);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] MovieDto dto)  // use [FromForm] instead of [FormBody] to get Poster from user
        {

            if (dto.Poster == null)
                return BadRequest("Poster is required!");

            if (!_allowedExtenstions.Contains(Path.GetExtension(dto.Poster.FileName).ToLower()))
                return BadRequest("Only .png and .jpg images are allowed!");

            if (dto.Poster.Length > _maxAllowedPosterSize)
                return BadRequest("Max allowed size for poster is 1MB!");

            var isValidGenre =await _genresService.IsValidGenre(dto.GenreId);
            if (!isValidGenre)
                return BadRequest("Invalid genere ID!");

            using var dataStream = new MemoryStream();
            await dto.Poster.CopyToAsync(dataStream);

            var movie = new Movie()
            {
                Title = dto.Title,
                Description = dto.Description,
                Year = dto.Year,
                Rate = dto.Rate,
                Poster = dataStream.ToArray(),
                GenreId = dto.GenreId
            };
            await _moviesService.AddMovie(movie);
            return Ok(movie);
        }


        [HttpPut(template:"{id}")]
        public async Task<IActionResult> UpdateAsync(int id,[FromForm]MovieDto dto) 
        {
            var movie = await _moviesService.GetById(id);

            if (movie == null) NotFound(value: $"no Movie is found with id: {id}");
            var isValidGenre = await _genresService.IsValidGenre(dto.GenreId);
            if (!isValidGenre)  return BadRequest("Invalid genere ID!");

            if (dto.Poster != null) 
            {
                if (!_allowedExtenstions.Contains(Path.GetExtension(dto.Poster.FileName).ToLower()))
                    return BadRequest("Only .png and .jpg images are allowed!");

                if (dto.Poster.Length > _maxAllowedPosterSize)
                    return BadRequest("Max allowed size for poster is 1MB!");

                using var dataStream = new MemoryStream();
                await dto.Poster.CopyToAsync(dataStream);

                movie.Poster = dataStream.ToArray();
            }

           

            movie.Title = dto.Title;
            movie.Description = dto.Description;
            movie.Year = dto.Year;
            movie.Rate = dto.Rate;

            _moviesService.Update(movie);
            return Ok(movie);

        }


        [HttpDelete(template: "{id}")]
        public async Task<IActionResult> DeleteAsync(int id) 
        {
            var movie =await _moviesService.GetById(id);

            if(movie == null) return NotFound(value: $"no Movie is found with Id: {id} to delete");
            
             _moviesService.Delete(movie);

            return Ok();
        }


    }
}