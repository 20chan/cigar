﻿using CreamRoll.Routing;
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
                PropertyNamingPolicy = new CamelBack(),
                PropertyNameCaseInsensitive = true,
            };
        }

        [Post("/papers")]
        public async Task<Response> CreatePaper(Request req) {
            try {
                var paper = await JsonSerializer.DeserializeAsync<Paper>(req.Body, options);
                Console.WriteLine(JsonSerializer.Serialize(paper));
                if (string.IsNullOrEmpty(paper.Nickname) || string.IsNullOrEmpty(paper.Body)) {
                    return ErrorResp("invalid nickname or body");
                }

                paper.Nickname = Encode(paper.Nickname);
                paper.Body = Encode(paper.Body);

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

        private static string Encode(string s) {
            return Microsoft.Security.Application.Encoder.HtmlEncode(s);
        }

        [Get("/papers")]
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
