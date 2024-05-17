import { Component, OnInit, ViewChild } from '@angular/core';
import { ITeam } from '../models/db/ITeam';
import { TeamSelectorComponent } from '../commonControls/team-selector/team-selector.component';

@Component({
  selector: 'app-staff-builder',
  templateUrl: './staff-builder.component.html',
  styleUrl: './staff-builder.component.scss',
})
export class StaffBuilderComponent {
  @ViewChild('teamSelector')
  teamSelector!: TeamSelectorComponent;
  dbTeamSelected: ITeam | undefined;

  public TeamAdded(): void {
    this.teamSelector.GetTeamsAutoComplete();
  }

  public TeamSelected(team: ITeam): void {
    this.dbTeamSelected = team;
  }
}
