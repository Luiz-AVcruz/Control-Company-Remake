using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ControlCompanyRemake.Core.Extentions;

public static class ExtensionMethods
{
	public static T GetFieldValue<T>(this object obj, string name)
	{
		return (T)(obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj));
	}

	public static void SetFieldValue<T>(this object obj, string method, T value)
	{
		obj.GetType().GetField(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty)?.SetValue(obj, value);
	}

	public static object Call(this object o, string methodName, params object[] args)
	{
		MethodInfo method = o.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (method != null)
		{
			return method.Invoke(o, args);
		}
		return null;
	}

	public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
	{
		if (dictionary.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
	{
		while (source.Any())
		{
			yield return source.Take(chunksize);
			source = source.Skip(chunksize);
		}
	}

	public static IEnumerable<T> DedupeBy<T>(this IEnumerable<T> source, Func<T, string> dedupeFunc)
	{
		return from t in source
			group t by dedupeFunc(t) into t
			select t.First();
	}

	public static void LogLayer(this GameObject gameObject)
	{
		ControlCompanyRemakePlugin.Instance.logger.LogMessage($"Enemy layer ({LayerMask.LayerToName(gameObject.layer)}): {gameObject.layer}");
	}

	public static void LogGameObjectName(this GameObject gameObject)
	{
		ControlCompanyRemakePlugin.Instance.logger.LogMessage(string.Format("GameObject name: {0}, parent: {1}", gameObject.name, (gameObject.transform.parent != null) ? gameObject.transform.parent.name : "null"));
	}

	public static void LogAllComponents(this GameObject gameObject)
	{
		Component[] components = gameObject.GetComponents<Component>();
		foreach (Component component in components)
		{
			_ = $"GameObject ({gameObject.name}) component: {component.name}";
		}
	}

	public static void LogPosition(this GameObject gameObject)
	{
		ControlCompanyRemakePlugin.Instance.logger.LogMessage($"GameObject ({gameObject.name}) position: {gameObject.transform.position}, local position: {gameObject.transform.localPosition}");
	}

	public static string RemoveChar(this string input, char charToRemove)
	{
		int num = input.IndexOf(charToRemove);
		if (num < 0)
		{
			return input;
		}
		return input.Remove(num, 1).RemoveChar(charToRemove);
	}

	public static bool IsObjectWithinAngle(this Transform transform, Transform other, Vector3 direction, float angleBounds)
	{
		Vector3 vector = other.position - transform.position;
		float f = Vector3.Angle(vector, direction);
		return Mathf.Abs(f) <= angleBounds;
	}
}
