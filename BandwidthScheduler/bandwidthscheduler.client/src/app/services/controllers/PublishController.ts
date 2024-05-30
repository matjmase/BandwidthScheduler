import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IScheduleProposalRequest } from '../../models/IScheduleProposalRequest';
import { IScheduleProposalResponse } from '../../models/IScheduleProposalResponse';
import { IScheduleSubmitRequest } from '../../models/IScheduleSubmitRequest';
import { ICommitment } from '../../models/db/ICommitment';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';
import { JsonCustom } from '../../models/JsonCustom';

export class PublishController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'publish/';
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

  public RequestScheduleTimes(
    request: IScheduleProposalRequest
  ): Observable<IScheduleProposalResponse> {
    return this.http.post<IScheduleProposalResponse>(
      this._baseUrl + 'proposal',
      request
    );
  }

  public SubmitSchedule(request: IScheduleSubmitRequest): Observable<void> {
    return this.http.post<void>(this._baseUrl + 'submit', request);
  }
}
