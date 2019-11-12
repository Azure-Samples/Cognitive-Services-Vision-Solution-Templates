import React, { Component } from 'react';
import { Col, Grid, Row } from 'react-bootstrap';

export class Layout extends Component {
  displayName = Layout.name

  render() {
    return (
      <Grid fluid>
            <Row>
                <Col sm={2}>   </Col>
                <Col lg={8}> {this.props.children}  </Col>
                <Col sm={2}>   </Col>
            </Row>
            
      </Grid>
    );
  }
}
