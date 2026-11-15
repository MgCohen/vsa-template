namespace VSA.Architecture;

public enum Access { Internal, Public }

public static class AccessOrder {
    public static bool IsWithin(this Access access, Access limit) => access <= limit;

    public static Access Narrowest(this Access first, Access second) => first <= second ? first : second;
}
