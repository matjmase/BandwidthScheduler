import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { TeamSelectorComponent } from '../commonControls/team-selector/team-selector.component';
import { ITeam } from '../models/db/ITeam';
import { SpinnerCardHorizontalStretch } from '../commonControls/spinner-card/spinner-card.component';
import { SpinnerCardContentsComponent } from './SpinnerCardContentsComponent';

@Component({
  template: '',
})
export abstract class TeamSelectorContainerComponent extends SpinnerCardContentsComponent {
  @ViewChild('teamSelector')
  public TeamSelector!: TeamSelectorComponent;

  @Output() TeamsUpdated: EventEmitter<void> = new EventEmitter<void>();

  public DbTeam: ITeam | undefined;

  public TeamCollectionUpdated(): void {
    this.DbTeam = undefined;
    this.TeamSelector.ResetForm();
    this.TeamSelector.GetTeamsAutoComplete();
    this.ResetTeamSelector();
  }

  public TeamSelected(team: ITeam): void {
    this.DbTeam = team;
    this.OnTeamSelected(team);
  }

  protected abstract OnTeamSelected(team: ITeam): void;
  protected abstract ResetTeamSelector(): void;
}
