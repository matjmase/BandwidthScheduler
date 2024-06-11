import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { INotificationResponse } from '../../models/INotificationResponse';
import { JsonCustom } from '../../models/JsonCustom';

export class NotificationController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'notification/';
  }

  public GetUnseenNotifications(
    take: number,
    skip: number
  ): Observable<INotificationResponse> {
    const headers = new HttpHeaders()
      .set('take', JsonCustom.stringify(take))
      .set('skip', JsonCustom.stringify(skip));
    return this.http.get<INotificationResponse>(this._baseUrl + 'notseen', {
      headers: headers,
    });
  }
  public GetAllNotifications(
    take: number,
    skip: number
  ): Observable<INotificationResponse> {
    const headers = new HttpHeaders()
      .set('take', JsonCustom.stringify(take))
      .set('skip', JsonCustom.stringify(skip));
    return this.http.get<INotificationResponse>(this._baseUrl + 'all', {
      headers: headers,
    });
  }
}
