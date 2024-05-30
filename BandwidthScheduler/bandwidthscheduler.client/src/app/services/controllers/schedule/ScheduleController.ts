import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DateTimeRangeSelectorModel } from '../../../commonControls/date-time-range-selector/date-time-range-selector-model';
import { JsonCustom } from '../../../models/JsonCustom';
import { ICommitment } from '../../../models/db/ICommitment';

export class ScheduleController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'schedule/';
  }

  public GetCommitments(
    range: DateTimeRangeSelectorModel,
    teamId: number
  ): Observable<ICommitment[]> {
    const headers = new HttpHeaders()
      .set('start', JsonCustom.stringify(range.start))
      .set('end', JsonCustom.stringify(range.end))
      .set('teamId', JsonCustom.stringify(teamId));
    return this.http.get<ICommitment[]>(this._baseUrl + 'commitments', {
      headers: headers,
    });
  }
}
