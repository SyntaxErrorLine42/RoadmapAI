using AutoMapper;
using RoadmapAI.Api.DTOs;
using RoadmapAI.Api.Models;
using RoadmapAI.Api.Models.Learning;

namespace RoadmapAI.Api.Mappings;

public class CourseProfile : Profile
{
    public CourseProfile()
    {
        CreateMap<Course, CourseSummaryDto>()
            .ForMember(dest => dest.MaterialCount, opt => opt.MapFrom(src => src.Materials.Count))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Course, CourseDetailDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CourseMaterial, CourseMaterialDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CourseRoadmap, CourseRoadmapDto>();

        CreateMap<RoadmapModule, RoadmapModuleDto>();

        CreateMap<RoadmapLesson, RoadmapLessonDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<RoadmapExam, RoadmapExamDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count));
    }
}
