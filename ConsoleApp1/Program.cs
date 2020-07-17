#if !NET452
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
#endif

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"reflection: {MeasureExecTime(ActivityTester.ActivityWithReflectionVoid)}");
            Console.WriteLine($"expression: {MeasureExecTime(ActivityTester.ActivityWithExpressionVoid)}");
            Console.WriteLine($"dynamicmethod: {MeasureExecTime(ActivityTester.ActivityWithDynamicMethodVoid)}");

            //var summary = BenchmarkRunner.Run<ActivityTester>();
            //var summary = BenchmarkRunner.Run<ActivityNameTester>();
            //Console.WriteLine(summary);
            //Console.ReadKey();
        }

        private static long MeasureExecTime(Action action)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            action();
            watch.Stop();
            return watch.ElapsedMilliseconds;
        }
    }

#if !NET452
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp21)]
    [SimpleJob(RuntimeMoniker.Net461)]
    [MemoryDiagnoser]
#endif
    public class ActivityTester
    {
        static PropertyInfo kindPropertyInfo = typeof(Activity).GetProperty("Kind");
        static Action<Activity, ActivityKind> setterNameProperty = CreateSetter("Kind");
        static Action<Activity, ActivityKind> kindSetterDynamicMethod = CreateSetterDynamicMethod();

#if !NET452
        [Benchmark]
#endif
        public Activity ActivityWithReflection()
        {
            Activity activity = new Activity("activity-with-reflection");

            kindPropertyInfo.SetValue(activity, ActivityKind.Client);

            return activity;
        }

        public static void ActivityWithReflectionVoid()
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                Activity activity = new Activity("activity-with-reflection");

                kindPropertyInfo.SetValue(activity, ActivityKind.Client);
            }
        }

#if !NET452
        [Benchmark]
#endif
        public Activity ActivityWithExpression()
        {
            var activity = new Activity("activity-with-expression");

            setterNameProperty(activity, ActivityKind.Client);

            return activity;
        }

        public static void ActivityWithExpressionVoid()
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                var activity = new Activity("activity-with-expression");

                setterNameProperty(activity, ActivityKind.Client);
            }
        }

#if !NET452
        [Benchmark]
#endif
        public Activity ActivityWithDynamicMethod()
        {
            var activity = new Activity("activity-with-dynamic-method");

            kindSetterDynamicMethod(activity, ActivityKind.Client);

            return activity;
        }

        public static void ActivityWithDynamicMethodVoid()
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                var activity = new Activity("activity-with-expression");

                kindSetterDynamicMethod(activity, ActivityKind.Client);
            }
        }

        public static Action<Activity, ActivityKind> CreateSetter(string name)
        {
            ParameterExpression instance = Expression.Parameter(typeof(Activity), "instance");
            ParameterExpression propertyValue = Expression.Parameter(typeof(ActivityKind), "propertyValue");

            var body = Expression.Assign(Expression.Property(instance, name), propertyValue);

            return Expression.Lambda<Action<Activity, ActivityKind>>(body, instance, propertyValue).Compile();
        }

        public static Action<Activity, ActivityKind> CreateSetterDynamicMethod()
        {
            DynamicMethod setterMethod = new DynamicMethod("Activity.Kind.Setter", null, new[] { typeof(Activity), typeof(ActivityKind) }, true);
            ILGenerator generator = setterMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, typeof(Activity).GetProperty("Kind", BindingFlags.Public | BindingFlags.Instance).SetMethod);
            generator.Emit(OpCodes.Ret);
            return (Action<Activity, ActivityKind>)setterMethod.CreateDelegate(typeof(Action<Activity, ActivityKind>));
        }
    }

}
