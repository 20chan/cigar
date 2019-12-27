using CreamRoll.Routing;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace cigar {
    public class CigarServer {
        private LiteDatabase db;
        private LiteCollection<Paper> papers;

        private static JsonSerializerOptions options;

        public CigarServer(string dbPath) {
            db = new LiteDatabase(dbPath);
            papers = db.GetCollection<Paper>("papers");

            options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
            };
        }

        [Post("/cigar/papers")]
        public async Task<Response> CreatePaper(Request req) {
            try {
                var paper = await JsonSerializer.DeserializeAsync<Paper>(req.Body, options);
                if (paper.Nickname == null || paper.Body == null) {
                    return ErrorResp("invalid nickname or body");
                }
                papers.Insert(paper);
                return new TextResponse("success");
            }
            catch (JsonException ex) {
                return ErrorResp("invalid json", ex);
            }
            catch (Exception ex) {
                return ErrorResp("server error", ex, StatusCode.InternalServerError);
            }
        }

        [Get("/cigar/papers")]
        public Response GetPapers(Request req) {
            return new JsonResponse(JsonSerializer.Serialize(papers.Find(p => p.Public), options));
        }

        private static JsonResponse ErrorResp(string message, Exception ex = null, StatusCode status = StatusCode.BadRequest) {
            return new JsonResponse(new JSON {
                ["message"] = message,
                ["ex"] = ex?.Message ?? "",
            }, status: status);
        }

        class JSON : Dictionary<string, object> {
            public static implicit operator string(JSON json) {
                return JsonSerializer.Serialize(json, options);
            }
        }

        class CamelBack : JsonNamingPolicy {
            public override string ConvertName(string name) {
                return $"{char.ToLower(name[0])}{name.Substring(1)}";
            }
        }

        public class Paper {
            public int Id { get; set; }
            public string Nickname { get; set; }
            public string Body { get; set; }
            public bool Public { get; set; }
        }
    }
}
