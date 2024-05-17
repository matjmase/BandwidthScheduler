export class JsonCustom {
  public static stringify(obj: any): string {
    return JSON.stringify(obj).replaceAll('"', '');
  }
}
